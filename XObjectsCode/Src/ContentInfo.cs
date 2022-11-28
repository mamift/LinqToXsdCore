using System.Collections.Generic;
using System.Linq;

namespace Xml.Schema.Linq.CodeGen;

public abstract partial class ContentInfo
{
    public ContentInfo lastChild;
    public ContentInfo nextSibling;
    protected ContentType contentType;
    protected Occurs occursInSchema; //The original occurence information in the XML schema

    public Occurs OccursInSchema
    {
        get { return occursInSchema; }
    }

    public bool IsOptional
    {
        get { return IsQMark || IsStar; }
    }

    public bool IsStar
    {
        get { return this.occursInSchema == Occurs.ZeroOrMore; }
    }

    public bool IsPlus
    {
        get { return this.occursInSchema == Occurs.OneOrMore; }
    }

    public bool IsQMark
    {
        get { return this.occursInSchema == Occurs.ZeroOrOne; }
    }


    public ContentType ContentType
    {
        get { return contentType; }
    }


    public IEnumerable<ContentInfo> Children
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

    public void AddChild(ContentInfo content)
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

    public string OccurenceString
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