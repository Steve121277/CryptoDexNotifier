using System;
using System.IO;
using System.Text;

namespace CryptoDexNotifier
{
	internal class Converter
	{
		// Constructors
		public Converter ()
		{
		}
		
		static Converter ()
		{
		}
		
		
		// Methods
		public static Converter GetInstance ()
		{
			if (Converter.m_instance == null)
			{
				Converter.m_instance = new Converter();
			}
			return Converter.m_instance;
		}
		
		public byte[] GetBytes (string strParam)
		{
			return Encoding.UTF8.GetBytes(strParam);
		}

        public byte[] GetBytes(bool bParam)
        {
            byte[] buffer1 = new byte[1];
            BinaryWriter writer1 = new BinaryWriter(new MemoryStream(buffer1));
            writer1.Write(bParam);
            writer1.Close();
            return buffer1;
        }
		
		public byte[] GetAsciiBytes (string strParam)
		{
			return Encoding.ASCII.GetBytes(strParam);
		}
		
		public byte[] GetBytes (byte nParam)
		{
			byte[] buffer1 = new byte[1];
			BinaryWriter writer1 = new BinaryWriter(new MemoryStream(buffer1));
			writer1.Write(nParam);
			writer1.Close();
			return buffer1;
		}
		
		public byte[] GetBytes (short nParam)
		{
			byte[] buffer1 = new byte[2];
			BinaryWriter writer1 = new BinaryWriter(new MemoryStream(buffer1));
			writer1.Write(nParam);
			writer1.Close();
			return buffer1;
		}
		
		public byte[] GetBytes (ushort nParam)
		{
			byte[] buffer1 = new byte[2];
			BinaryWriter writer1 = new BinaryWriter(new MemoryStream(buffer1));
			writer1.Write(nParam);
			writer1.Close();
			return buffer1;
		}
		
		public byte[] GetBytes (int nParam)
		{
			byte[] buffer1 = new byte[4];
			BinaryWriter writer1 = new BinaryWriter(new MemoryStream(buffer1));
			writer1.Write(nParam);
			writer1.Close();
			return buffer1;
		}

        public byte[] GetBytes(Int64 nParam)
        {
            byte[] buffer1 = new byte[8];
            BinaryWriter writer1 = new BinaryWriter(new MemoryStream(buffer1));
            writer1.Write(nParam);
            writer1.Close();
            return buffer1;
        }
		
		public byte[] GetBytes (uint nParam)
		{
			byte[] buffer1 = new byte[4];
			BinaryWriter writer1 = new BinaryWriter(new MemoryStream(buffer1));
			writer1.Write(nParam);
			writer1.Close();
			return buffer1;
		}
		
		public string GetString (byte[] param)
		{
			return Encoding.UTF8.GetString(param);
		}
		
		public byte GetByte (byte[] param)
		{
			return new BinaryReader(new MemoryStream(param)).ReadByte();
		}
		
		public short GetInt16 (byte[] param)
		{
			return new BinaryReader(new MemoryStream(param)).ReadInt16();
		}
		
		public ushort GetUInt16 (byte[] param)
		{
			return new BinaryReader(new MemoryStream(param)).ReadUInt16();
		}
		
		public int GetInt32 (byte[] param)
		{
			return new BinaryReader(new MemoryStream(param)).ReadInt32();
		}
		
		public uint GetUInt32 (byte[] param)
		{
			return new BinaryReader(new MemoryStream(param)).ReadUInt32();
		}
		
		public string Base16encode (string asContent)
		{
			string str0 = "";
			for (int i = 0;i < asContent.Length; i++)
			{
				int arg0 = asContent[i];
				str0 = str0 + string.Format("{0:X4}", arg0);
			}
			return str0;
		}
		
		public string Base16decode (string asContent)
		{
			string arg0 = "";
			for (int startIndex = 0;startIndex < asContent.Length; startIndex += 4)
			{
				char arg1 = Convert.ToChar(Convert.ToUInt16(asContent.Substring(startIndex, 4), 0x10));
				arg0 = arg0 + arg1;
			}
			return arg0;
		}
		
        public Int32 ToInt32(float fVal)
        {
            try
            {
                return Convert.ToInt32(fVal);
            }
            catch (System.Exception)
            {
                return 0;
            }
        }

        public Int32 ToInt32(string str)
        {
            try
            {
                return Convert.ToInt32(str);
            }
            catch (System.Exception)
            {
                return 0;
            }
        }

        public UInt32 ToUInt32(string str)
        {
            try
            {
                return Convert.ToUInt32(str);
            }
            catch (System.Exception)
            {
                return 0;
            }
        }

        public byte ToInt8(string str)
        {
            try
            {
                return Convert.ToByte(str);
            }
            catch (System.Exception)
            {
                return 0;
            }
        }

        public Int64 ToInt64(string str)
        {
            try
            {
                return Convert.ToInt64(str);
            }
            catch (System.Exception)
            {
                return 0;
            }
        }

        public double ToDouble(object str)
        {
            try
            {
                return Convert.ToDouble(str);
            }
            catch (System.Exception)
            {
                return 0;
            }
        }
		
		// Statics
		private static Converter m_instance;
	}
}
