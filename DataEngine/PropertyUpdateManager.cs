using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataEngine
{
    /// <summary>
    /// Reuses the same PropertyUpdateSection to avoid dynamic memory allocation
    /// each time when a property changes.
    /// </summary>
    public class PropertyUpdateManager
    {
        static NotificationObject.PropertyUpdateSection currentSection = new NotificationObject.PropertyUpdateSection();

        public static IDisposable StartUpdate<T>(NotificationObject notification_object, Expression<Func<T>> propertyExpression)
        {
            var property_name = PropertySupport.ExtractPropertyName(propertyExpression);

            return StartUpdate(notification_object, property_name);
        }

        public static IDisposable StartUpdate(NotificationObject notification_object, string property_name)
        {
            currentSection.Start(notification_object, property_name);

            return currentSection;
        }

        public static bool IsUpdateInProgress
        {
            get
            {
                return currentSection.IsStarted;
            }
        }
    }
}
