﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using A2v10.Web.Mvc;
    using Microsoft.AspNet.Identity;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Shared/Exception.cshtml")]
    public partial class _Views_Shared_Exception_cshtml : System.Web.Mvc.WebViewPage<System.Exception>
    {
        public _Views_Shared_Exception_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 3 "..\..\Views\Shared\Exception.cshtml"
  
	Layout = null;

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta");

WriteLiteral(" charset=\"utf-8\"");

WriteLiteral(" />\r\n    <meta");

WriteLiteral(" name=\"viewport\"");

WriteLiteral(" content=\"width=device-width, initial-scale=1.0\"");

WriteLiteral(@">
    <title>Exception - A2:Web</title>
    <style>
        body {
            margin: 1.67em;
            font-family: system-ui, 'Segoe UI', Tahoma, Verdana, sans-serif;
            font-size:13px;
            color:#333;
        }
        .trace {
            font-family: Consolas, 'Courier New', Courier, monospace;
            font-size:12px;
        }
        .text-danger {
            color: #a94442;
        }
        h1, h2 {
            margin:0.2em, 0;
        }
        h5 {
            margin:0;
        }
    </style>
</head>
<body>
    <h1");

WriteLiteral(" class=\"text-danger\"");

WriteLiteral(">An exception occurred</h1>\r\n    <h2");

WriteLiteral(" class=\"text-danger\"");

WriteLiteral(">");

            
            #line 37 "..\..\Views\Shared\Exception.cshtml"
                       Write(Model.Message);

            
            #line default
            #line hidden
WriteLiteral("</h2>\r\n\r\n    <h5>Stack trace</h5>\r\n    <p");

WriteLiteral(" class=\"trace\"");

WriteLiteral(">\r\n");

WriteLiteral("        ");

            
            #line 41 "..\..\Views\Shared\Exception.cshtml"
   Write(Model.StackTrace);

            
            #line default
            #line hidden
WriteLiteral("\r\n    </p>\r\n</body>\r\n</html>\r\n");

        }
    }
}
#pragma warning restore 1591
