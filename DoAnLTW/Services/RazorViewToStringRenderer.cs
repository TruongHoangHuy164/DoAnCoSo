using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DoAnLTW.Services
{
    public interface IRazorViewToStringRenderer
    {
        Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model);
    }

    public class RazorViewToStringRenderer : IRazorViewToStringRenderer
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public RazorViewToStringRenderer(
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model)
        {
            var actionContext = new ActionContext(
                new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = _serviceProvider },
                new RouteData(),
                new ActionDescriptor()
            );

            var viewEngineResult = _razorViewEngine.FindView(actionContext, viewName, false);

            if (!viewEngineResult.Success)
            {
                throw new ArgumentException($"Không tìm thấy view '{viewName}'", nameof(viewName));
            }

            var viewContext = new ViewContext(
                actionContext,
                viewEngineResult.View,
                new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                },
                new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                new StringWriter(),
                new HtmlHelperOptions()
            );

            await viewEngineResult.View.RenderAsync(viewContext);
            return viewContext.Writer.ToString();
        }
    }
}