using Microsoft.Graph.Models;
using ObjectsComparer;
using Pursuit.Model;

namespace Pursuit.Utilities
{
    public class ADRecordComparersFactory : ComparersFactory
    {
        public override ObjectsComparer.IComparer<T> GetObjectsComparer<T>(ComparisonSettings settings = null, BaseComparer parentComparer = null)
        {
            if (typeof(T) == typeof(ADRecord))
            {
                var comparer = new ObjectsComparer.Comparer<ADRecord>(settings, parentComparer, this);
                //Do not compare PersonId  
                comparer.AddComparerOverride<Guid>(DoNotCompareValueComparer.Instance);
                //Skipping date 
                //comparer.AddComparerOverride<DateTime>(DoNotCompareValueComparer.Instance);

                //Do not compare Id  
                comparer.AddComparerOverride(() => new ADRecord().Id, DoNotCompareValueComparer.Instance);

                comparer.IgnoreMember(() => new ADRecord().Role);
                comparer.IgnoreMember(() => new ADRecord().whencreated);

                comparer.AddComparerOverride(
                    () => new ADRecord().Phone, new PhoneNumberComparer());
                return (ObjectsComparer.IComparer<T>)comparer;
            }
            return base.GetObjectsComparer<T>(settings, parentComparer);
        }
    }

    public class PhoneNumberComparer : AbstractValueComparer<string>
    {
        public override bool Compare(string obj1, string obj2, ComparisonSettings settings)
        {
            return ExtractDigits(obj1) == ExtractDigits(obj2);
        }
        private string ExtractDigits(string str)
        {
            return string.Join(string.Empty, (str ?? string.Empty).ToCharArray().Where(char.IsDigit));
        }
    }

}