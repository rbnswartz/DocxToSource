using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Serialize.OpenXml.CodeGen;

namespace DocxToSource.Models;

public class PartTreeItem: TreeItemBase
{
    private readonly IdPartPair part;

    public PartTreeItem(IdPartPair idPartPair): base()
    {
        Header =
            $"[{idPartPair.RelationshipId}] {idPartPair.OpenXmlPart.Uri} ({idPartPair.OpenXmlPart.GetType().Name})";
         part = idPartPair;
         foreach (var i in part.OpenXmlPart.Parts)
         {
             Items.Add(new PartTreeItem(i));
         }

         if (part.OpenXmlPart.RootElement != null)
         {
             Items.AddRange(BuildXmlElementTreeItem(new []{part.OpenXmlPart.RootElement}));
         }
    }


    public override string BuildCodeDomTextDocument(CodeDomProvider provider)
    {
        return part.OpenXmlPart.GenerateSourceCode(provider);
    }
}