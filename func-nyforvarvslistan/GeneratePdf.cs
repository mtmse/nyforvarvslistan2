using System;
using HtmlRendererCore.PdfSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace func_nyforvarvslistan
{
    public class PdfGenerator
    {
        public void GeneratePdf(string htmlContent, string outputPath)
        {
            var pdf = HtmlRendererCore.PdfSharp.PdfGenerator.GeneratePdf(htmlContent, PdfSharpCore.PageSize.A4);
            pdf.Save(outputPath);
        }
    }
}
