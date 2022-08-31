using System.CodeDom.Compiler;
using DocumentFormat.OpenXml.Packaging;
using Serialize.OpenXml.CodeGen;

namespace DocxToSource.Models;

public class PackageTreeItem: TreeItemBase
{
    private readonly OpenXmlPackage _package;

    public PackageTreeItem(OpenXmlPackage package): base()
    {
        _package = package;
        foreach (IdPartPair p in _package.Parts)
        {
            Items.Add(new PartTreeItem(p));
        }
    }

    public override string BuildCodeDomTextDocument(CodeDomProvider provider)
    {
        return _package.GenerateSourceCode(provider);
    }
}