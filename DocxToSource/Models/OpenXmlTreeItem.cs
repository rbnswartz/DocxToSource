using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using Serialize.OpenXml.CodeGen;

namespace DocxToSource.Models;

public class OpenXmlTreeItem: TreeItemBase
{
    private readonly OpenXmlElement element;

    public OpenXmlTreeItem(OpenXmlElement e): base()
    {
        element = e;
        Items.AddRange(BuildXmlElementTreeItem(e.Elements()));
    }
    
    public override string BuildXmlTextDocument()
    {
        StringBuilder sb = new();

        using (StringWriter writer = new(sb))
        {
            using XmlTextWriter xTarget = new(writer);
            XmlDocument xDoc = new();
            xDoc.LoadXml(element.OuterXml);

            xTarget.Formatting = Formatting.Indented;
            xTarget.Indentation = 2;

            xDoc.Normalize();
            xDoc.PreserveWhitespace = true;
            xDoc.WriteContentTo(xTarget);

            xTarget.Flush();
            xTarget.Close();
        }

        return sb.ToString();
    }
    
    public override string BuildCodeDomTextDocument(CodeDomProvider provider)
    {
        return element.GenerateSourceCode(provider);
    }
}