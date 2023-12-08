using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum Relation
    {
        NONE,
        CHILD,
        SIBLING,
        PARENT,
        REPEATING
    }
    public enum HtmlAttr
    {
        Div,
        InnerText,
        H1,
        H2,
        H3,
        H4,
        Class,
        Href,
        A,
        Id,
        Strong,
        Li,
        Ul,
        Article,
        Title,
        _SharpText,
        Table,
        THead,
        TBody,
        Th,
        Tr,
        Td,
        Rowspan,
        Colspan,
        Section,
        Main,
        P,
        Span,
        Role,
        Textlength
    }

    public enum Action
    {
        REMOVE,
        REPLACE,
        SPLITONCE,
        REMOVEINSENSITIVE
    }

    internal enum TableSection
    {
        HEADER,
        BODY
    }

}
