using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Extensions.Logging;

namespace RevitMcpServer
{
    /// <summary>
    /// Wrapper class for Revit API operations that handles exceptions and threading
    /// </summary>
    public class RevitApiWrapper
    {
        private readonly Application _app;
        private readonly UIApplication _uiApp;
        private readonly ILogger<RevitApiWrapper> _logger;

        public RevitApiWrapper(Application app, UIApplication uiApp, ILogger<RevitApiWrapper> logger)
        {
            _app = app;
            _uiApp = uiApp;
            _logger = logger;
        }

        /// <summary>
        /// Executes a Revit API action in the main Revit thread
        /// </summary>
        public T ExecuteInRevitContext<T>(Func<T> action)
        {
            T result = default;
            Exception exception = null;

            // Use the ExternalEvent pattern to execute in Revit's context
            var uiApplication = _uiApp;
            var revitEvent = new RevitEventHandler<T>(action);
            uiApplication.Application.DocumentChanged += revitEvent.Execute;

            try
            {
                // Trigger a dummy transaction to force the DocumentChanged event
                using (var doc = uiApplication.ActiveUIDocument.Document)
                using (var transaction = new Transaction(doc, "MCP Event Trigger"))
                {
                    transaction.Start();
                    // Do nothing, just trigger the event
                    transaction.RollBack();
                }

                // Wait for the event to complete
                revitEvent.WaitForCompletion();

                // Get the result or exception
                result = revitEvent.Result;
                exception = revitEvent.Exception;
            }
            finally
            {
                uiApplication.Application.DocumentChanged -= revitEvent.Execute;
            }

            if (exception != null)
            {
                _logger.LogError(exception, "Error executing Revit API operation");
                throw exception;
            }

            return result;
        }

        /// <summary>
        /// Get elements by category
        /// </summary>
        public IList<ElementModel> GetElementsByCategory(string categoryName)
        {
            return ExecuteInRevitContext(() =>
            {
                var doc = _uiApp.ActiveUIDocument.Document;
                var elements = new List<ElementModel>();

                // Find the built-in category
                BuiltInCategory builtInCategory;
                if (!Enum.TryParse<BuiltInCategory>("OST_" + categoryName, out builtInCategory))
                {
                    _logger.LogWarning($"Category {categoryName} not found");
                    return elements;
                }

                // Get elements of the specified category
                var collector = new FilteredElementCollector(doc)
                    .OfCategory(builtInCategory)
                    .WhereElementIsNotElementType();

                foreach (var element in collector)
                {
                    elements.Add(ConvertToElementModel(element));
                }

                return elements;
            });
        }

        /// <summary>
        /// Get elements by type
        /// </summary>
        public IList<ElementModel> GetElementsByType(string typeName)
        {
            return ExecuteInRevitContext(() =>
            {
                var doc = _uiApp.ActiveUIDocument.Document;
                var elements = new List<ElementModel>();

                // Get elements of the specified type
                var collector = new FilteredElementCollector(doc)
                    .OfClass(Type.GetType($"Autodesk.Revit.DB.{typeName}"))
                    .WhereElementIsNotElementType();

                foreach (var element in collector)
                {
                    elements.Add(ConvertToElementModel(element));
                }

                return elements;
            });
        }

        /// <summary>
        /// Modify an element parameter
        /// </summary>
        public bool ModifyElementParameter(int elementId, string parameterName, string value)
        {
            return ExecuteInRevitContext(() =>
            {
                var doc = _uiApp.ActiveUIDocument.Document;
                var element = doc.GetElement(new ElementId(elementId));

                if (element == null)
                {
                    _logger.LogWarning($"Element with ID {elementId} not found");
                    return false;
                }

                var parameter = element.GetParameters(parameterName).FirstOrDefault();
                if (parameter == null)
                {
                    _logger.LogWarning($"Parameter {parameterName} not found on element {elementId}");
                    return false;
                }

                using (var transaction = new Transaction(doc, "Modify Parameter"))
                {
                    transaction.Start();

                    // Set parameter based on its storage type
                    switch (parameter.StorageType)
                    {
                        case StorageType.String:
                            parameter.Set(value);
                            break;
                        case StorageType.Integer:
                            parameter.Set(int.Parse(value));
                            break;
                        case StorageType.Double:
                            parameter.Set(double.Parse(value));
                            break;
                        case StorageType.ElementId:
                            parameter.Set(new ElementId(int.Parse(value)));
                            break;
                        default:
                            _logger.LogWarning($"Unsupported parameter type: {parameter.StorageType}");
                            transaction.RollBack();
                            return false;
                    }

                    transaction.Commit();
                    return true;
                }
            });
        }

        /// <summary>
        /// Convert a Revit element to a simplified model
        /// </summary>
        private ElementModel ConvertToElementModel(Element element)
        {
            var model = new ElementModel
            {
                Id = element.Id.IntegerValue,
                Name = element.Name,
                Category = element.Category?.Name,
                Parameters = new Dictionary<string, string>()
            };

            // Get all parameters
            foreach (Parameter param in element.Parameters)
            {
                if (!param.HasValue) continue;

                string value = "";
                switch (param.StorageType)
                {
                    case StorageType.String:
                        value = param.AsString();
                        break;
                    case StorageType.Integer:
                        value = param.AsInteger().ToString();
                        break;
                    case StorageType.Double:
                        value = param.AsDouble().ToString();
                        break;
                    case StorageType.ElementId:
                        value = param.AsElementId().IntegerValue.ToString();
                        break;
                }

                model.Parameters[param.Definition.Name] = value;
            }

            return model;
        }
    }

    /// <summary>
    /// Helper class for executing actions in Revit's context
    /// </summary>
    internal class RevitEventHandler<T>
    {
        private readonly Func<T> _action;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public T Result { get; private set; }
        public Exception Exception { get; private set; }

        public RevitEventHandler(Func<T> action)
        {
            _action = action;
        }

        public void Execute(object sender, DocumentChangedEventArgs args)
        {
            try
            {
                Result = _action();
            }
            catch (Exception ex)
            {
                Exception = ex;
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public void WaitForCompletion()
        {
            _resetEvent.WaitOne();
        }
    }

    /// <summary>
    /// Simple model to represent a Revit element
    /// </summary>
    public class ElementModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }
}
