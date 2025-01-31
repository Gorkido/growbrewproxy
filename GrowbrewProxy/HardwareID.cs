﻿using System.Text;

namespace GrowbrewProxy
{
    public class HardwareID
    {
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                _ = sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static string GetHwid()
        {
            string cpuID = identifier("win32_processor", "processorID");
            string hStrMd = CreateMD5(cpuID);
            return hStrMd;
        }

        public static string identifier(string wmiClass, string wmiProperty)
        {
            string result = "";
            System.Management.ManagementClass mc =
        new(wmiClass);
            System.Management.ManagementObjectCollection moc = mc.GetInstances();
            foreach (System.Management.ManagementObject mo in moc)
            {
                //Only get the first one
                if (result == "")
                {
                    try
                    {
                        result = mo[wmiProperty].ToString();
                        break;
                    }
                    catch
                    {
                    }
                }
            }
            return result;
        }
    }
}
