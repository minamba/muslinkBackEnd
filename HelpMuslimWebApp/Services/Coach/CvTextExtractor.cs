using System.IO.Compression;
using System.Text;
using System.Xml;

namespace HelpMuslimWebApp.Api.Services.Coach
{
    // Texte d'un CV Word (.docx) : paragraphes de word/document.xml, dans l'ordre. Les PDF sont lus directement par le modèle ;
    // l'ancien format Word (.doc) n'est pas lisible (null).
    public static class CvTextExtractor
    {
        private const string WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        public static string? ExtractDocx(Stream docx)
        {
            try
            {
                using var zip = new ZipArchive(docx, ZipArchiveMode.Read, leaveOpen: true);
                // Certains outils écrivent les chemins de l'archive avec des barres obliques inversées
                var entry = zip.Entries.FirstOrDefault(e => e.FullName.Replace('\\', '/').Equals("word/document.xml", StringComparison.OrdinalIgnoreCase));
                if (entry == null) return null;

                using var reader = XmlReader.Create(entry.Open(), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
                var text = new StringBuilder();
                var paragraph = new StringBuilder();
                while (reader.Read())
                {
                    if (reader.NamespaceURI != WordNamespace) continue;
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.LocalName)
                        {
                            case "t": paragraph.Append(reader.ReadElementContentAsString()); break;
                            case "tab": paragraph.Append('\t'); break;
                            case "br": paragraph.Append('\n'); break;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "p")
                    {
                        var line = paragraph.ToString().Trim();
                        if (line.Length > 0) text.AppendLine(line);
                        paragraph.Clear();
                    }
                }
                var result = text.ToString().Trim();
                return result.Length == 0 ? null : result;
            }
            catch (Exception ex) when (ex is InvalidDataException or XmlException)
            {
                return null;
            }
        }
    }
}
