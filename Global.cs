using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CryptoDexNotifier
{
    public static class Global
    {
        public const int INVALID_ID = -1;

        //public const string UKCode = "ED";
        //public const string CanadaCode = "DC";
        //public const string GermanCode = "DE";
        //public const string FrenchCode = "FR";
        //public const string ItalianCode = "IT";
        //public const string SpanishCode = "ES";

        public const string SV_UK = "AmazonUK";
        public const string SV_CA = "Amazon_Canada";
        public const string SV_DE = "AmazonDE";
        public const string SV_FR = "AmazonFR";
        public const string SV_IT = "AmazonIT";
        public const string SV_SP = "AmazonSP";
        public const string SV_US = "AmazonUS";

        public static CategoryNum Int2Category(int category)
        {
            CategoryNum res;
            switch (category)
            {
                case 1:
                    res = CategoryNum.Baby;
                    break;
                case 2:
                    res = CategoryNum.Beauty;
                    break;
                case 3:
                    res = CategoryNum.HomeAndKitchen;
                    break;
                case 4:
                    res = CategoryNum.Jewellery;
                    break;
                case 5:
                    res = CategoryNum.Sports;
                    break;
                case 6:
                    res = CategoryNum.Toys;
                    break;
                case 7:
                    res = CategoryNum.Watches;
                    break;
                case 8:
                    res = CategoryNum.Unknown;
                    break;
                case 9:
                    res = CategoryNum.Books;
                    break;
                case 10:
                    res = CategoryNum.Office;
                    break;
                default:
                    res = CategoryNum.Unknown;
                    break;
            }

            return res;
        }
    }

    public enum CategoryNum
    {
        Baby = 1,
        Beauty = 2,
        HomeAndKitchen = 3,
        Jewellery = 4,
        Sports = 5,
        Toys = 6,
        Watches = 7,
        Unknown = 8,
        Books = 9,
        Office = 10,
    }

    public static class StringExtensions
    {
        public static string Left(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            maxLength = Math.Abs(maxLength);

            return (value.Length <= maxLength
                   ? value
                   : value.Substring(0, maxLength)
                   );
        }
        public static string ConvertToEuroFormat(string value)
        {
            return value.Replace(".", ",");
        }
    }
}