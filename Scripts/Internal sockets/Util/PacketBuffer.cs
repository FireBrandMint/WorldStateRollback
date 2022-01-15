using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class PacketBuffer : IDisposable
{
    List <byte> bufferList;
    byte [] readbuffer;

    int readpos;

    bool buffupdate = false;

    public PacketBuffer ()
    {
        bufferList = new List<byte>();

        readpos = 0;

        //if (bytes != null) WriteBytes(bytes);
    }

    public PacketBuffer (byte[] bytes)
    {
        bufferList = new List<byte>(bytes);
        buffupdate = true;

        readpos = 0;

        //if (bytes != null) WriteBytes(bytes);
    }

    public int GetReadPosition ()
    {
        return readpos;
    }

    public byte [] ToArray () => bufferList.ToArray();

    public int Count ()
    {
        return bufferList.Count;
    }

    public int Length ()
    {
        return Count() - readpos;
    }

    public bool DoneReading ()
    {
        return readpos >= bufferList.Count;
    }

    public void Clear ()
        {
            bufferList.Clear();

            buffupdate = true;

            readpos = 0;
        }
    
        public void WriteBytes (byte[] input)
        {
            if (input.Length == 0) return;

            bufferList.AddRange(input);
            buffupdate = true;
        }

        public void WriteByte (byte input)
        {
            bufferList.Add(input);
            buffupdate = true;
        }
    
        public void WriteInteger (int input)
        {
            bufferList.AddRange(BitConverter.GetBytes(input));
            buffupdate = true;
        }

        public void WriteFloat (float input)
        {
            bufferList.AddRange(BitConverter.GetBytes(input));
            buffupdate = true;
        }

        public void WriteString (string input)
        {
            bufferList.AddRange(BitConverter.GetBytes(input.Length));
            bufferList.AddRange(Encoding.ASCII.GetBytes(input));
            buffupdate = true;
        }

        public void WriteBool (bool input)
        {
            bufferList.Add(input? (byte)1:(byte)0);
            buffupdate = true;
        }

        //Read data

        public int ReadInteger (bool peek = true)
        {
            if (bufferList.Count > readpos)
            {
                if (buffupdate)
                {
                    readbuffer = bufferList.ToArray();
                    buffupdate = false;
                }

                int value = BitConverter.ToInt32(readbuffer, readpos);

                if (peek & bufferList.Count > readpos)
                {
                    readpos += 4;
                }

                return value;
            }

            throw new Exception ("Buffer is past its limit!");
        }

        public float ReadFloat (bool peek = true)
        {
            if (bufferList.Count > readpos)
            {
                if (buffupdate)
                {
                    readbuffer = bufferList.ToArray();
                    buffupdate = false;
                }

                float value = BitConverter.ToSingle(readbuffer, readpos);

                if (peek & bufferList.Count > readpos)
                {
                    readpos += 4;
                }

                return value;
            }

            throw new Exception ("Buffer is past its limit!");
        }

        public byte ReadByte (bool peek = true)
        {
            if (bufferList.Count > readpos)
            {
                if (buffupdate)
                {
                    readbuffer = bufferList.ToArray();
                    buffupdate = false;
                }

                byte value = readbuffer[readpos];

                if (peek & bufferList.Count > readpos)
                {
                    readpos += 1;
                }

                return value;
            }

            throw new Exception ("Buffer is past its limit!");
        }

        public byte[] ReadBytes (int length, bool peek = true)
        {
            if (bufferList.Count > readpos)
            {
                if (buffupdate)
                {
                    readbuffer = bufferList.ToArray();
                    buffupdate = false;
                }

                byte[] value = bufferList.GetRange(readpos, length).ToArray();

                if (peek & bufferList.Count > readpos)
                {
                    readpos += length;
                }

                return value;
            }

            throw new Exception ("Buffer is past its limit!");
        }

        public string ReadString (bool peek = true)
        {

            int length = ReadInteger(true);
            if (buffupdate)
            {
                readbuffer = bufferList.ToArray();
                buffupdate = false;
            }

            string value = Encoding.ASCII.GetString(readbuffer, readpos, length);

            if (peek & bufferList.Count > readpos)
            {
                readpos += length;
            }

            return value;
        }

        public bool ReadBool (bool peek = true)
        {
            if (bufferList.Count > readpos)
            {
                if (buffupdate)
                {
                    readbuffer = bufferList.ToArray();
                    buffupdate = false;
                }

                byte value = readbuffer[readpos];

                if (peek & bufferList.Count > readpos)
                {
                    readpos += 1;
                }

                return value == 1;
            }

            throw new Exception ("Buffer is past its limit!");
        }

        public void WriteObjects (object[] objects)
        {
            WriteInteger(objects.Length);
            for (int i = 0; i<objects.Length; ++i)
            {
                switch (objects[i])
                {
                    case float f: WriteByte(0); WriteFloat(f); break;
                    case int integer: WriteByte(1); WriteInteger(integer); break;
                    case bool b: WriteByte(2); byte t = 0; if (b) t = 1; WriteByte(t); break;
                    case byte bi: WriteByte(3); WriteByte(bi); break;
                    case string s: WriteByte(4); WriteString(s); break;
                    default: WriteByte(100); break;
                }
            }
        }

        public object[] ReadObjects ()
        {
            int lenght = ReadInteger();
            var objects = new object[lenght];

            for (int i = 0; i<lenght; ++i)
            {
                byte type = ReadByte();
                switch (type)
                {
                    case 0: objects[i] = ReadFloat(); break;
                    case 1: objects[i] = ReadInteger(); break;
                    case 2: objects[i] = ReadByte() == (byte)1; break;
                    case 3: objects[i] = ReadByte(); break;
                    case 4: objects[i] = ReadString(); break;
                    default: break;
                }
            }

            return objects;
        }
    
        //IDisposable
        private bool disposedValue = false;
        protected virtual void Dispose (bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    bufferList.Clear();
                }
                readpos = 0;
            }

            disposedValue = true;
        }

        public void Dispose ()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
}
