using System.Collections.Generic;
using System.Linq;

namespace Xml.Schema.Linq.CodeGen;

internal abstract partial class ContentInfo
{
    internal ContentInfo lastChild;
    internal ContentInfo nextSibling;
    protected ContentType contentType;
    protected Occurs occursInSchema; //The original occurence information in the XML schema

    internal Occurs OccursInSchema
    {
        get { return occursInSchema; }
    }

    internal bool IsOptional
    {
        get { return IsQMark || IsStar; }
    }

    internal bool IsStar
    {
        get { return this.occursInSchema == Occurs.ZeroOrMore; }
    }

    internal bool IsPlus
    {
        get { return this.occursInSchema == Occurs.OneOrMore; }
    }

    internal bool IsQMark
    {
        get { return this.occursInSchema == Occurs.ZeroOrOne; }
    }


    internal ContentType ContentType
    {
        get { return contentType; }
    }


    internal IEnumerable<ContentInfo> Children
    {
        get
        {
            ContentInfo current = lastChild;
            while (current != null)
            {
                current = current.nextSibling;
                yield return current;
                if (current == lastChild)
                {
                    yield break;
                }
            }
        }
    }

    internal void AddChild(ContentInfo content)
    {
        if (lastChild == null)
        {
            content.nextSibling = content;
        }
        else
        {
            content.nextSibling = lastChild.nextSibling;
            lastChild.nextSibling = content;
        }

        lastChild = content;
    }

    internal string OccurenceString
    {
        get
        {
            if (this.IsStar)
            {
                return "*";
            }
            else if (this.IsPlus)
            {
                return "+";
            }
            else if (this.IsQMark)
            {
                return "?";
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public override string ToString()
    {
        return this.Children.Any() ? $"{{ {string.Join(", ", this.Children.Select(x => x.ToString()))} }}{this.OccurenceString}" : string.Empty;
    }
}