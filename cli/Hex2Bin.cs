using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

public class Hex2Bin
{
  //  ERROR TYPE DEFINITION
  const int ERR_FILE_NOT_FOUND    = 1;
  const int ERR_BAD_FILE_FORMAT   = 2;
  const int ERR_BAD_CHECKSUM      = 3;
  const int ERR_NO_EOF            = 4;
  const int ERR_WRONG_EOF         = 5;
  const int ERR_EOF_WRONG_POS     = 6;
  string[] err_string =
  {
    "file not found.",
    "bad hex file format.",
    "wrong checksum in hex file.",
    "\"end of file\" not found in hex file.",
    "too much \"end of file\" in hex file.",
    "\"end of file\" wrong position."
  };
  UInt32 line_numb;
  int bytecount , rectype, data;
  UInt32 address;
  string checksum, datastr;
  byte[] filearray;
  int EOF_ok = 0;
  UInt32 ELAR_start = 0;
  UInt32 ELAR_addr = 0;
  string line;
  string filecheck = "";
  UInt32 FWLEN = 342016;

  public Hex2Bin() {
    filearray = new byte[FWLEN];
    int i;
    for (i = 0; i < FWLEN; i++)
    filearray[i] = 255;
  }

  public static void Main(string[] args) {
    string file = null;
    foreach(string arg in args) {
      if(file == null) {
        file = arg;
      }
    }

    if(file != null) {
      Hex2Bin obj = new Hex2Bin();
      obj.processFile(file);
    }else{
      Console.WriteLine("please provide the hex file without .hex");
    }
  }


  /********************************************************************
  * int ValLine(string line2eval)
  * function to evaluate a single line of the .hex file. The function
  * takes as parameter one line of the .hex file, parses all the
  * fields and controls the checksum.
  *
  * Return:
  * an int value indicating the report for the function. 0 for
  * operation executed with success, other values for some kind of
  * error
  * ****************************************************************/
  private int ValLine(string line2eval)
  {
    //  CHECK #1: control for the ':' start char and for line minimum length
    if ((line2eval.IndexOf(':') != 0) || (line2eval.Length < 11))
    return ERR_BAD_FILE_FORMAT;
    line2eval = line2eval.Substring(1);
    try
    {
      bytecount = Convert.ToInt16(line2eval.Substring(0, 2),16);
    }
    catch
    {
      return ERR_BAD_FILE_FORMAT;
    }

    //  CHECK #2: for the effective line length
    if (line2eval.Length != (10 + bytecount * 2))
    return ERR_BAD_FILE_FORMAT;

    //  CHECK #3: checksum control
    int i, checkcalc = 0;
    for (i = 0; i < (line2eval.Length - 2); i = i + 2)
    {
      try
      {
        checkcalc += Convert.ToInt16(line2eval.Substring(i, 2), 16);
      }
      catch
      {
        return ERR_BAD_FILE_FORMAT;
      }
    }
    checkcalc = 1 + ~checkcalc;
    checksum = line2eval.Substring(line2eval.Length - 2, 2);
    string checkcomp = checkcalc.ToString("x8").Substring(6,2);
    if (checksum.ToUpper() != checkcomp.ToUpper())
    return ERR_BAD_CHECKSUM;

    //  The length of the line  and the checksum is ok, so we can
    //  proceed reading the other fields: address, record type and data
    try
    {
      address = Convert.ToUInt32(line2eval.Substring(2, 4), 16);
      rectype = Convert.ToInt16(line2eval.Substring(6, 2), 16);
    }
    catch
    {
      return ERR_BAD_FILE_FORMAT;
    }
    datastr = line2eval.Substring(8, line2eval.Length - 10);

    return 0;
  }


  /********************************************************************
  * int CreateBin()
  * function to create the bin structure. This function writes the data
  * inside the array filearray[]. Then can be written directly inside
  * a binary file.
  *
  * Return:
  * an int value indicating the report for the function. 0 for
  * operation executed with success, other values for some kind of
  * error
  * ****************************************************************/
  private int CreateBin()
  {
    //  Handling of different record types
    int a1 = 0;
    switch (rectype)
    {
      //  00 - DATA RECORD
      case 0:
      //  Extended linear addressing
      ELAR_addr = (ELAR_start * 65536) + address;
      while (datastr.Length > 0) {
        try {
          data = Convert.ToInt16(datastr.Substring(0, 2), 16);
        } catch {
          return ERR_BAD_FILE_FORMAT;
        }
        datastr = datastr.Substring(2);

        //  Is a phantom byte?
        if ( ((ELAR_addr + 1) % 4 != 0) && (ELAR_addr < FWLEN)) {
          filearray[ELAR_addr] = (byte)data;
        }
        ELAR_addr++;
      }
      break;

      //  01 - END OF FILE RECORD
      case 1:
      //  Only one EOF per file allowed!
      EOF_ok++;
      break;

      //  04 - EXTENDED LINEAR ADDRESS RECORD
      case 4:
      if (datastr.Length != 4) {
        return ERR_BAD_FILE_FORMAT;
      }
      try {
        ELAR_start = Convert.ToUInt32 (datastr, 16);
        if (ELAR_start != 0)
        a1++;
      } catch {
        return ERR_BAD_FILE_FORMAT;
      }
      break;

    }

    return 0;
  }


  /********************************************************************
  * Convert2bin(StreamReader filetoconv)
  * function to convert the specified hex file into bin. the function
  * firstly will perform a validation of the hex file (format and
  * checksum) and then will convert it into binary.
  *
  * Return:
  * an int value indicating the report for the function. 0 for
  * operation executed with success, other values for some kind of
  * error
  * ****************************************************************/
  private int Convert2bin(StreamReader filetoconv)
  {
    int retval;

    //  Check for correct file format


    EOF_ok = 0;
    ELAR_start = 0;
    ELAR_addr = 0;
    line_numb = 0;
    Console.WriteLine("Parsing hex file... ");
    //  Single line reading and parsing of file
    while ((line = filetoconv.ReadLine()) != null)
    {
      if (line != "")
      {
        if (EOF_ok != 0)
        return ERR_EOF_WRONG_POS;
        //  Validating the hex file
        retval = ValLine(line);
        if (retval != 0)
        return retval;
        CreateBin();
        //tbTest.Text += line;
        line_numb++;
      }
    }
    if (EOF_ok < 1) {
      return ERR_NO_EOF;
    } else if (EOF_ok > 1) {
      return ERR_WRONG_EOF;
    }
    Console.WriteLine("OK");
    //  Opening save dialog

    //  Saving file
    string bin = filecheck + ".bin";
    string md5 = filecheck + ".md5";

    FileStream file_bin = new FileStream(bin, FileMode.Create, FileAccess.Write);
    BinaryWriter stream_bin = new BinaryWriter(file_bin);
    int i;
    int i_count = 0,cont=0;
    for (i = 0; i < FWLEN; i++)
    {
      cont++;
      i_count++;
      if (i_count < 4)
      stream_bin.Write(filearray[i]);
      else
      i_count = 0;
    }
    stream_bin.Close();
    Console.WriteLine("OK");

    //  MD5 calculation and saving into file
    string hashText = "";
    string hexValue = "";
    Console.WriteLine("MD5 calculation and file saving... ");
    byte[] fileData = File.ReadAllBytes(file_bin.Name);
    byte[] hashData = MD5.Create().ComputeHash(fileData);
    foreach (byte b in hashData) {
      hexValue = b.ToString("X").ToLower();
      hashText += (hexValue.Length == 1 ? "0" : "") + hexValue;
    }

    FileStream file_md5 = new FileStream(md5, FileMode.Create, FileAccess.Write);
    BinaryWriter stream_md5 = new BinaryWriter(file_md5);
    string hexconv;
    for (i = 0; i < 32; i = i + 2)
    {
      hexconv = hashText.Substring(i, 2);
      int value = Convert.ToInt32(hexconv, 16);
      stream_md5.Write((byte) value);
    }
    stream_md5.Close();
    Console.WriteLine("OK");

    return 0;
  }


  private void processFile(string filename) {
    string extension = ".hex";
    string hex = filename;

    filecheck = filename;


    if(hex.EndsWith(extension)) {
      int index = filecheck.Length - extension.Length;
      filecheck = filecheck.Remove(index, extension.Length);
    }

    if (System.IO.File.Exists(hex)) {
      StreamReader file = new StreamReader(hex);

      int valfile = Convert2bin(file);
      file.Close();
      if (valfile > 0 && valfile < err_string.Length) {
        Console.WriteLine("ERROR: " + err_string[valfile-1]);
      }
    } else {
      Console.WriteLine("ERROR: file not found");
    }
  }
}
