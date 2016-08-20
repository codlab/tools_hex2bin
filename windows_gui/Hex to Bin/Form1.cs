using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;



namespace Hex_to_Bin
{
    public partial class Form1 : Form
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
        bool ELAR_on = false;
        string filecheck = "";
        UInt32 FWLEN = 342016;

        public Form1()
        {
            InitializeComponent();
            filearray = new byte[FWLEN];
            int i;
            for (i = 0; i < FWLEN; i++)
                filearray[i] = 255;
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
                    while (datastr.Length > 0)
                    {
                        try
                        {
                            data = Convert.ToInt16(datastr.Substring(0, 2), 16);
                        }
                        catch
                        {
                            return ERR_BAD_FILE_FORMAT;
                        }
                        datastr = datastr.Substring(2);

                        //  Is a phantom byte?
                        if ( ((ELAR_addr + 1) % 4 != 0) && (ELAR_addr < FWLEN))
                        {
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
                    if (datastr.Length != 4)
                        return ERR_BAD_FILE_FORMAT;
                    ELAR_on = true;
                    try
                    {
                        ELAR_start = Convert.ToUInt32 (datastr, 16);
                        if (ELAR_start != 0)
                            a1++;
                    }
                    catch
                    {
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
            ELAR_on = false;
            ELAR_addr = 0;
            line_numb = 0;
            tbOut.Text += "Parsing hex file... ";
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
            if (EOF_ok < 1)
                return ERR_NO_EOF;
            else if (EOF_ok > 1)
                return ERR_WRONG_EOF;
            tbOut.Text += "OK\r\n";
            //  Opening save dialog
            tbOut.Text += "Saving .bin file... ";
            SaveFileDialog dlg_binfilesave = new SaveFileDialog();
            dlg_binfilesave.Filter = "Binary file|*.bin";
            dlg_binfilesave.Title = "Choose file name and location";
            dlg_binfilesave.ShowDialog();

            //  Saving file
            if (dlg_binfilesave.FileName != "")
            {

                FileStream file_bin = new FileStream(dlg_binfilesave.FileName, FileMode.Create, FileAccess.Write);
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
                tbOut.Text += "OK\r\n";

                //  MD5 calculation and saving into file
                if (chkMD5.Checked)
                {
                    string hashText = "";
                    string hexValue = "";
                    tbOut.Text += "MD5 calculation and file saving... ";
                    byte[] fileData = File.ReadAllBytes(file_bin.Name);
                    byte[] hashData = MD5.Create().ComputeHash(fileData);
                    foreach (byte b in hashData)
                    {
                        hexValue = b.ToString("X").ToLower();
                        hashText += (hexValue.Length == 1 ? "0" : "") + hexValue;
                    }

                    //  Now the MD5 key is saved into another file
                    //  Opening save dialog
                    SaveFileDialog dlg_binfilesavemd5 = new SaveFileDialog();
                    dlg_binfilesavemd5.Filter = "Binary file|*.md5";
                    dlg_binfilesavemd5.Title = "Choose file name and location for the md5 key";
                    dlg_binfilesavemd5.ShowDialog();
                    if (dlg_binfilesavemd5.FileName != "")
                    {
                        FileStream file_md5 = new FileStream(dlg_binfilesavemd5.FileName, FileMode.Create, FileAccess.Write);
                        BinaryWriter stream_md5 = new BinaryWriter(file_md5);
                        string hexconv;
                        for (i = 0; i < 32; i = i + 2)
                        {
                            hexconv = hashText.Substring(i, 2);
                            int value = Convert.ToInt32(hexconv, 16);
                            stream_md5.Write((byte) value);
                        }
                        stream_md5.Close();
                        tbOut.Text += "OK\r\n";
                    }
                    else
                        tbOut.Text += "ERR\r\n";
                }
            }
            else
                tbOut.Text += "ERR\r\n";

            return 0;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            int valfile;
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.DefaultExt = "hex";
            openFile.Filter = "Hex files (*.hex)|*.hex";
            openFile.ShowDialog();
            if (openFile.FileNames.Length > 0)
            {
                foreach (string filename in openFile.FileNames)
                {
                    // Insert code here to process the files.
                    filecheck = filename;
                }



                tbOut.Text = "";
                //  FILE CONVERSION
                StreamReader file = new StreamReader(filecheck);
                //  Check for file existance
                if (!System.IO.File.Exists(filecheck))
                    MessageBox.Show("ERROR: file not found");
                else
                {
                    valfile = Convert2bin(file);
                    file.Close();
                    if (valfile != 0)
                        tbOut.Text = tbOut.Text + "ERROR: " + err_string[valfile-1];
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
