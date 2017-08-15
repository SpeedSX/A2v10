﻿using A2v10.Infrastructure;
using A2v10.Web.Mvc.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace A2v10.Web.Mvc.Controllers
{
    [Authorize]
    public class BaseController : Controller
    {
        protected IApplicationHost _host;
        protected IDbContext _dbContext;
        protected IRenderer _renderer;

        public Int64 UserId
        {
            get
            {
                return User.Identity.GetUserId<Int64>();
            }
        }

        public BaseController(IApplicationHost host, IDbContext dbContext, IRenderer renderer)
        {
            _host = host;
            _dbContext = dbContext;
            _renderer = renderer;
        }

        protected async Task RenderElementKind(RequestUrlKind kind, String pathInfo)
        {
            Response.ContentType = "text/html";
            Response.ContentEncoding = Encoding.UTF8;
            try
            {
                RequestModel rm = await RequestModel.CreateFromUrl(_host, kind, pathInfo);
                RequestView rw = rm.CurrentAction as RequestView;
                await Render(rw);
            }
            catch (Exception ex)
            {
                Response.Output.Write($"$<div>{ex.Message}</div>");
            }
        }

        protected async Task Render(RequestView rw)
        {
            String viewName = rw.GetView();
            String loadProc = rw.LoadProcedure;
            IDataModel model = null;
            if (loadProc != null)
            {
                //TODO: // use model ID
                model = await _dbContext.LoadModelAsync(loadProc, new
                {
                    UserId = UserId
                });
            }
            String fileName = _host.MakeFullPath(rw.Path, rw.GetView() + ".xaml");
            String rootId = "el" + Guid.NewGuid().ToString();
            using (var strWriter = new StringWriter())
            {
                var ri = new RenderInfo()
                {
                    RootId = rootId,
                    FileName = fileName,
                    Writer = strWriter
                };
                _renderer.Render(ri);
                // write markup
                Response.Output.Write(strWriter.ToString());
            }
            if (model != null)
            {
                // write model script
                String templateText = null;
                String dataModelText = "null";
                if (rw.template != null)
                {
                    templateText = await _host.ReadTextFile(rw.Path, rw.template + ".js");
                }
                var jss = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    NullValueHandling = NullValueHandling.Ignore
                };
                dataModelText = JsonConvert.SerializeObject(model.Root, jss);

                //Response.Output.Write(model.GetModelScript(rootId, templateFile));
                const String script =
@"
<script type=""text/javascript"">
(function() {

    function getModelData() {
        return $(DataModelText);
    };


    new Vue({
        el:'#$(RootId)',
        data: getModelData()
    });
})();
</script>
";
                var sb = new StringBuilder(script);
                sb.Replace("$(RootId)", rootId);
                sb.Replace("$(DataModelText)", dataModelText);
                Response.Output.Write(sb.ToString());
            }
        }
    }
}
