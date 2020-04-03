using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DemoCentral.Helpers
{
    /// <summary>
    /// Binder used to bind a comma seperated list of numbers to a List<long>.
    /// 
    /// Code is adapated from a (non-working) generic csv.
    /// See https://asp.net-hacker.rocks/2018/10/17/customizing-aspnetcore-08-modelbinders.html for more info.
    /// </summary>
    public class CsvModelBinder : ModelBinderAttribute, IModelBinder
    {
        public CsvModelBinder() : base(typeof(CsvModelBinder)) { }
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // Specify a default argument name if none is set by ModelBinderAttribute
            var modelName = bindingContext.ModelName;
            if (String.IsNullOrEmpty(modelName))
            {
                modelName = "model";
            }

            // Try to fetch the value of the argument by name
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;
            // Check if the argument value is null or empty
            if (String.IsNullOrEmpty(value))
            {
                bindingContext.Result = ModelBindingResult.Success(new List<long>());
                return Task.CompletedTask;
            }

            var model = value.Split(',').Select(long.Parse).ToList();

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
