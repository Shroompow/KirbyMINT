﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MINT;
using MINT.TDX;

namespace MINT.TDX
{
    public class Script
    {
        public List<string> script = new List<string>();
        public List<byte[]> compScript = new List<byte[]>();

        public Script(byte[] script, uint[] hashIds, string[] hashNames)
        {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(script)))
            {
                Read(reader, hashIds, hashNames);
            }
        }
        public Script(string[] script)
        {
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                Write(writer);
            }
        }

        public void Read(BinaryReader reader, uint[] hashIds, string[] hashNames)
        {
            reader.BaseStream.Seek(0x10, SeekOrigin.Begin);
            uint scriptnameoffset = reader.ReadUInt32();
            uint sdatalist = reader.ReadUInt32();
            uint xreflist = reader.ReadUInt32();
            uint classlist = reader.ReadUInt32();
            reader.BaseStream.Seek(scriptnameoffset, SeekOrigin.Begin);
            uint scriptnamelen = reader.ReadUInt32();
            string scriptname = string.Join("", reader.ReadChars((int)scriptnamelen));
            script.Add("script " + scriptname);
            script.Add("{");
            reader.BaseStream.Seek(sdatalist, SeekOrigin.Begin);
            uint sdatalen = reader.ReadUInt32();
            byte[] sdata = reader.ReadBytes((int)sdatalen);
            reader.BaseStream.Seek(xreflist, SeekOrigin.Begin);
            uint xrefcount = reader.ReadUInt32();
            List<string> xref = new List<string>();
            for (int i = 0; i < xrefcount; i++)
            {
                byte[] hash = reader.ReadBytes(0x4);
                uint xrefHash = uint.Parse(BitConverter.ToUInt32(hash, 0).ToString("X8"), System.Globalization.NumberStyles.HexNumber);
                if (hashIds.Length > 0 && hashIds.Contains(xrefHash))
                {
                    xref.Add(hashNames[hashIds.ToList().IndexOf(xrefHash)]);
                }
                else
                {
                    xref.Add(xrefHash.ToString("X8"));
                }
            }
            reader.BaseStream.Seek(classlist, SeekOrigin.Begin);
            uint classcount = reader.ReadUInt32();
            List<uint> classoffsets = new List<uint>();
            for (int i = 0; i < classcount; i++)
            {
                classoffsets.Add(reader.ReadUInt32());
            }
            for (int i = 0; i < classcount; i++)
            {
                reader.BaseStream.Seek(classoffsets[i], SeekOrigin.Begin);
                uint nameoffset = reader.ReadUInt32();
                byte[] hash = reader.ReadBytes(0x4);
                uint varlist = reader.ReadUInt32();
                uint methodlist = reader.ReadUInt32();
                uint constlist = reader.ReadUInt32();
                uint flags = reader.ReadUInt32();
                reader.BaseStream.Seek(nameoffset, SeekOrigin.Begin);
                uint namelen = reader.ReadUInt32();
                string name = string.Join("", reader.ReadChars((int)namelen));
                script.Add("\n    [" + BitConverter.ToUInt32(hash, 0).ToString("X8") + "] (" + flags + ")");
                script.Add("    class " + name);
                script.Add("    {");
                reader.BaseStream.Seek(varlist, SeekOrigin.Begin);
                uint varcount = reader.ReadUInt32();
                List<uint> varoffsets = new List<uint>();
                for (int v = 0; v < varcount; v++)
                {
                    varoffsets.Add(reader.ReadUInt32());
                }
                for (int v = 0; v < varcount; v++)
                {
                    reader.BaseStream.Seek(varoffsets[v], SeekOrigin.Begin);
                    uint varnameoffset = reader.ReadUInt32();
                    byte[] varhash = reader.ReadBytes(0x4);
                    uint vartypeoffset = reader.ReadUInt32();
                    uint varflags = reader.ReadUInt32();
                    reader.BaseStream.Seek(varnameoffset, SeekOrigin.Begin);
                    uint varnamelen = reader.ReadUInt32();
                    string varname = string.Join("", reader.ReadChars((int)varnamelen));
                    reader.BaseStream.Seek(vartypeoffset, SeekOrigin.Begin);
                    uint vartypelen = reader.ReadUInt32();
                    string vartype = string.Join("", reader.ReadChars((int)vartypelen));
                    script.Add("        [#" + BitConverter.ToUInt32(varhash, 0).ToString("X8") + "] (" + varflags + ") " + vartype + " " + varname);
                }
                reader.BaseStream.Seek(constlist, SeekOrigin.Begin);
                uint constcount = reader.ReadUInt32();
                List<uint> constoffsets = new List<uint>();
                for (int c = 0; c < constcount; c++)
                {
                    constoffsets.Add(reader.ReadUInt32());
                }
                for (int c = 0; c < constcount; c++)
                {
                    reader.BaseStream.Seek(constoffsets[c], SeekOrigin.Begin);
                    uint constnameoffset = reader.ReadUInt32();
                    uint constval = reader.ReadUInt32();
                    reader.BaseStream.Seek(constnameoffset, SeekOrigin.Begin);
                    uint constnamelen = reader.ReadUInt32();
                    string constname = string.Join("", reader.ReadChars((int)constnamelen));
                    script.Add("        const " + constname + " = " + constval);
                }
                reader.BaseStream.Seek(methodlist, SeekOrigin.Begin);
                uint methodcount = reader.ReadUInt32();
                List<uint> methodoffsets = new List<uint>();
                for (int m = 0; m < methodcount; m++)
                {
                    methodoffsets.Add(reader.ReadUInt32());
                }
                for (int m = 0; m < methodcount; m++)
                {
                    reader.BaseStream.Seek(methodoffsets[m], SeekOrigin.Begin);
                    uint methodnameoffset = reader.ReadUInt32();
                    byte[] methodhash = reader.ReadBytes(0x4);
                    uint methoddataoffset = reader.ReadUInt32();
                    uint methodflags = reader.ReadUInt32();
                    reader.BaseStream.Seek(methodnameoffset, SeekOrigin.Begin);
                    uint methodnamelen = reader.ReadUInt32();
                    string methodname = string.Join("", reader.ReadChars((int)methodnamelen));
                    script.Add("\n        [" + BitConverter.ToUInt32(methodhash, 0).ToString("X8") + "] (" + methodflags + ")");
                    script.Add("        " + methodname);
                    script.Add("        {");
                    reader.BaseStream.Seek(methoddataoffset, SeekOrigin.Begin);
                    Opcodes opcodes = new Opcodes();
                    for (int b = 0; b < reader.BaseStream.Length; b++)
                    {
                        byte w = reader.ReadByte();
                        byte z = reader.ReadByte();
                        byte x = reader.ReadByte();
                        byte y = reader.ReadByte();
                        ushort v = BitConverter.ToUInt16(new byte[] { x, y }, 0);
                        if (opcodes.opcodeNames.Keys.Contains(w))
                        {
                            string cmd = "            " + opcodes.opcodeNames[w];
                            switch (opcodes.opcodeFormats[w])
                            {
                                case Format.None:
                                    {
                                        break;
                                    }
                                case Format.Z:
                                    {
                                        cmd += $" r{z.ToString("X2")}";
                                        break;
                                    }
                                case Format.X:
                                    {
                                        cmd += $" r{x.ToString("X2")}";
                                        break;
                                    }
                                case Format.Y:
                                    {
                                        cmd += $" r{y.ToString("X2")}";
                                        break;
                                    }
                                case Format.sV:
                                    {
                                        cmd += $" 0x{BitConverter.ToUInt32(sdata, v).ToString("X")}";
                                        break;
                                    }
                                case Format.sZV:
                                    {
                                        cmd += $" r{z.ToString("X2")}, 0x{BitConverter.ToUInt32(sdata, v).ToString("X")}";
                                        break;
                                    }
                                case Format.strV:
                                    {
                                        string strV = "";
                                        for (int s = v; s < sdata.Length; s++)
                                        {
                                            if (sdata[s] != 0x00)
                                            {
                                                strV += Encoding.UTF8.GetChars(sdata, s, 1);
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        cmd += $" \"{strV}\"";
                                        break;
                                    }
                                case Format.strZV:
                                    {
                                        string strV = "";
                                        for (int s = v; s < sdata.Length; s++)
                                        {
                                            if (sdata[s] != 0x00)
                                            {
                                                strV += Encoding.UTF8.GetString(sdata, s, 1);
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        cmd += $" r{z.ToString("X2")}, \"{strV}\"";
                                        break;
                                    }
                                case Format.xV:
                                    {
                                        cmd += $" {xref[v]}";
                                        break;
                                    }
                                case Format.xZV:
                                    {
                                        cmd += $" r{z.ToString("X2")}, {xref[v]}";
                                        break;
                                    }
                                case Format.ZX:
                                    {
                                        cmd += $" r{z.ToString("X2")}, r{x.ToString("X2")}";
                                        break;
                                    }
                                case Format.aZX:
                                    {
                                        cmd += $" [{z.ToString("X2")}] r{x.ToString("X2")}";
                                        break;
                                    }
                                case Format.aaZX:
                                    {
                                        cmd += $" [{z.ToString("X2")} {x.ToString("X2")}]";
                                        break;
                                    }
                                case Format.ZY:
                                    {
                                        cmd += $" r{z.ToString("X2")}, r{y.ToString("X2")}";
                                        break;
                                    }
                                case Format.XY:
                                    {
                                        cmd += $" r{x.ToString("X2")}, r{y.ToString("X2")}";
                                        break;
                                    }
                                case Format.ZXY:
                                    {
                                        cmd += $" r{z.ToString("X2")}, r{x.ToString("X2")}, r{y.ToString("X2")}";
                                        break;
                                    }
                                case Format.nZXY:
                                    {
                                        cmd += $" {z.ToString("X2")}, {x.ToString("X2")}, {y.ToString("X2")}";
                                        break;
                                    }
                                case Format.aZXY:
                                    {
                                        cmd += $" [{z.ToString("X2")}] r{x.ToString("X2")}, r{y.ToString("X2")}";
                                        break;
                                    }
                                case Format.ZXxY:
                                    {
                                        cmd += $" r{z.ToString("X2")}, r{x.ToString("X2")}, {xref[y]}";
                                        break;
                                    }
                            }
                            script.Add(cmd);
                        }
                        else
                        {
                            script.Add("            " + w.ToString("X2") + z.ToString("X2") + x.ToString("X2") + y.ToString("X2"));
                        }
                        if (w == 0x47 || w == 0x48)
                        {
                            break;
                        }
                    }
                    script.Add("        }");
                }
                script.Add("    }");
            }
            script.Add("}");
        }

        public void Write(BinaryWriter writer)
        {

        }
    }
}