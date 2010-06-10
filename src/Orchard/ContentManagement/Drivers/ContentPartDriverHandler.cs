using System.Collections.Generic;
using JetBrains.Annotations;
using Orchard.ContentManagement.Handlers;
using Orchard.Logging;

namespace Orchard.ContentManagement.Drivers {
    [UsedImplicitly]
    public class ContentPartDriverHandler : ContentHandlerBase {
        private readonly IEnumerable<IContentPartDriver> _drivers;

        public ContentPartDriverHandler(IEnumerable<IContentPartDriver> drivers) {
            _drivers = drivers;
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public override void BuildDisplayModel(BuildDisplayModelContext context) {
            _drivers.Invoke(driver => {
                                var result = driver.BuildDisplayModel(context);
                                if (result != null)
                                    result.Apply(context);
                            }, Logger);
        }

        public override void BuildEditorModel(BuildEditorModelContext context) {
            _drivers.Invoke(driver => {
                                var result = driver.BuildEditorModel(context);
                                if (result != null)
                                    result.Apply(context);
                            }, Logger);
        }

        public override void UpdateEditorModel(UpdateEditorModelContext context) {
            _drivers.Invoke(driver => {
                                var result = driver.UpdateEditorModel(context);
                                if (result != null)
                                    result.Apply(context);
                            }, Logger);
        }
    }
}