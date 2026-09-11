using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using GromCore.Laser.Titan.Math;

namespace GromCore.Laser.Logic.Util
{
    public static class GlobalUtil
    {

        public const string Hashtag = "#";
        public const string StringCharacters = "ABDEFGHIJKLMNPQRSTUVWXYZabdefghijklmnpqrstuvwxyz123456789";
        public const string ConversionTagCharacters = "289PYLQGRJCUV";
        public static void Skip()
        {
            ;
        }

        public static string GetIpFromDomain(string domain)
        {
            return domain[..domain.IndexOf(':')];
        }

        public static Dictionary<TK, TV> Merge<TK, TV>(IEnumerable<Dictionary<TK, TV>> dictionaries) where TK : notnull
        {
            var result = new Dictionary<TK, TV>();
            {
                foreach (var dict in dictionaries) dict.ToList().ForEach(pair => result.Add(pair.Key, pair.Value));
            }

            return result;
        }

        public static string RandomString(int length)
        {
            var result = new char[length];
            for (var i = 0; i < result.Length; i++)
                result[i] = StringCharacters[new Random().Next(StringCharacters.Length)];
            return new string(result);
        }

        public static string ConvertStringToUnderscore(string input)
        {
            var charArray = input.ToCharArray();
            for (var i = 0; i < charArray.Length; i++)
                if (charArray[i] == '_' && i < charArray.Length - 1)
                {
                    charArray[i] = char.ToUpper(charArray[i + 1]);
                    {
                        Array.Copy(charArray, i + 2, charArray, i + 1, charArray.Length - i - 2);
                        Array.Resize(ref charArray, charArray.Length - 1);
                    }
                }
                else if (i == 0)
                {
                    charArray[i] = char.ToLower(charArray[i]);
                }

            return new string(charArray);
        }

        public static string GenerateToken(long id)
        {
            return RandomString(10) + id * 3 + RandomString(id < 100 ? 50 : 100);
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static string GenerateScIdToken(long id)
        {
            return "#SC-" + "PU" + id + "/" + RandomString(10) + id * 3 + ":";
        }

        public static int GetPortFromDomain(string domain)
        {
            return Convert.ToInt32(domain[(domain.IndexOf(':') + 1)..]);
        }

        public static IPEndPoint GetFullyEndPointByDomain(string domain)
        {
            return new IPEndPoint(IPAddress.Parse(GetIpFromDomain(domain)), GetPortFromDomain(domain));
        }


        public static int GenerateRandomIntForBetween(int min, int max)
        {
            return new Random().Next(min, max + 1);
        }

        public static bool GetChanceByPercentage(int percentage)
        {
            return new Random().Next(0, 100) <= percentage;
        }

        public static string? GetIpBySocket(Socket socket)
        {
            return socket.RemoteEndPoint!.ToString()?
                [..socket.RemoteEndPoint.ToString()!.IndexOf(":", StringComparison.Ordinal)];
        }

        public static int? GetPortBySocket(Socket socket)
        {
            return Convert.ToInt32(socket.RemoteEndPoint!.ToString()?
                [(socket.RemoteEndPoint.ToString()!.IndexOf(':') + 1)..]);
        }

        public static string ConvertStringToCamelCase(string str)
        {
            var result = new StringBuilder();
            {
                var capitalizeNext = false;
                {
                    foreach (var currentChar in str)
                        if (currentChar == '_')
                        {
                            capitalizeNext = true;
                        }
                        else
                        {
                            if (capitalizeNext)
                            {
                                result.Append(char.ToUpper(currentChar));
                                capitalizeNext = false;
                            }
                            else
                            {
                                result.Append(char.ToLower(currentChar));
                            }
                        }
                }
            }

            return result.ToString();
        }

        public static object GetTypeAndSetOfGetDefaultValueToJson(FieldInfo field)
        {
            try
            {
                if (field.FieldType == typeof(string)) return "NULL";

                if (field.FieldType == typeof(int) || field.FieldType == typeof(long) ||
                    field.FieldType == typeof(byte)) return -1;

                if (field.FieldType == typeof(bool)) return false;

                if (field.FieldType == typeof(double)) return (double)-1;

                if (field.FieldType == typeof(short)) return (short)-1;

                if (field.FieldType == typeof(float)) return -1f;

                if (field.FieldType == typeof(char)) return '\u0000';

                if (field.FieldType == typeof(Dictionary<object, object>)) return new Dictionary<object, object>();

                if (field.FieldType.IsArray)
                {
                    var componentType = field.FieldType.GetElementType();
                    {
                        if (componentType == typeof(int)) return Array.Empty<int>();

                        if (componentType == typeof(string)) return Array.Empty<string>();

                        if (componentType == typeof(char)) return Array.Empty<char>();

                        if (componentType == typeof(long)) return Array.Empty<long>();

                        if (componentType == typeof(byte)) return Array.Empty<byte>();

                        if (componentType == typeof(Dictionary<object, object>)) return new Dictionary<object, object>();
                    }
                }
            }
            catch (Exception)
            {
                // ignored.
            }

            return null!;
        }


        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        public static int AngleMutation(int mutationStart, int mutationCounter, int mutationSpread, int mutationCount,
            int mutationStep)
        {
            mutationSpread = -mutationSpread + mutationStep;

            var deltaMutator = mutationSpread != 0 ? -mutationSpread / 4 : 0;
            {
                for (var i = 0; i < mutationCounter; i++)
                    deltaMutator += mutationSpread != 0 ? mutationSpread / (2 * mutationCount) : 4;
            }

            return mutationStart + deltaMutator + 360 / 100;
        }

        public static List<int> SumRepeatedElements(IEnumerable<int> inputList)
        {
            var enumerable = inputList as int[] ?? inputList.ToArray();
            {
                if (enumerable.Length < 3) return enumerable.ToList();
            }

            var elementSumDictionary = new Dictionary<int, int>();

            foreach (var number in enumerable.ToList().Where(number => !elementSumDictionary.TryAdd(number, number)))
                elementSumDictionary[number] += number;
            return elementSumDictionary.Values.ToList();
        }

        public static int GetChangeNameCostByCount(int count)
        {
            return Math.Min(Math.Max(count, 0) * 30, 120);
        }

        public static bool GetIsCorrectName(string nameForCheck)
        {
            return nameForCheck.Length is > 3 and <= 15;
        }

        public static int GetCurrentTimeInSecondsSinceEpoch()
        {
            return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static void Destructor(object @class)
        {
            var fields = @class.GetType().GetFields();
            {
                foreach (var field in fields)
                    try
                    {
                        field.SetValue(@class, GetTypeAndSetOfGetDefaultValueToJson(field));
                    }
                    catch (Exception)
                    {
                        // ignored.
                    }
            }
        }
    }
}
