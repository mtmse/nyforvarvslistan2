using System.Text;
using System.IO;
using System.IO.Compression;

namespace func_nyforvarvslistan
{
    public class EpubGenerator
    {
        public void GenerateEpub(string htmlContent, string outputPath)
        {
            using (var fileStream = new FileStream(outputPath, FileMode.Create))
            {
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
                {
                    var mimetypeEntry = archive.CreateEntry("mimetype");
                    using (var writer = new StreamWriter(mimetypeEntry.Open(), Encoding.ASCII))
                    {
                        writer.Write("application/epub+zip");
                    }

                    var containerEntry = archive.CreateEntry("META-INF/container.xml");
                    using (var writer = new StreamWriter(containerEntry.Open(), Encoding.UTF8))
                    {
                        writer.Write(@"<?xml version=""1.0""?>
                            <container version=""1.0"" xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"">
                              <rootfiles>
                                <rootfile full-path=""OPS/content.opf"" media-type=""application/oebps-package+xml""/>
                              </rootfiles>
                            </container>");
                    }

                    var contentOpfEntry = archive.CreateEntry("OPS/content.opf");
                    using (var writer = new StreamWriter(contentOpfEntry.Open(), Encoding.UTF8))
                    {
                        writer.Write(@"<?xml version=""1.0""?>
                            <package xmlns=""http://www.idpf.org/2007/opf"" version=""2.0"" unique-identifier=""BookId"">
                              <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:opf=""http://www.idpf.org/2007/opf"">
                              </metadata>
                              <manifest>
                                <item id=""content"" href=""content.html"" media-type=""application/xhtml+xml"" />
                              </manifest>
                              <spine toc=""ncx"">
                                <itemref idref=""content"" />
                              </spine>
                            </package>");
                    }

                    var contentHtmlEntry = archive.CreateEntry("OPS/content.html");
                    using (var writer = new StreamWriter(contentHtmlEntry.Open(), Encoding.UTF8))
                    {
                        writer.Write(htmlContent);
                    }
                }
            }
        }
    }

}
