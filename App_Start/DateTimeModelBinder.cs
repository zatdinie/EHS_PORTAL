using System;
using System.Globalization;
using System.Web.Mvc;

namespace EHS_PORTAL
{
    public class DateTimeModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == null)
            {
                return null;
            }

            var value = valueProviderResult.AttemptedValue;
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            // Try to parse the date using our custom format
            if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime;
            }
            
            // If we can't parse it in our format, try standard formats
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out dateTime))
            {
                return dateTime;
            }

            // If all else fails, add a model error
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, 
                $"'{value}' is not a valid date. Please use format dd/MM/yyyy.");
            
            return null;
        }
    }
} 