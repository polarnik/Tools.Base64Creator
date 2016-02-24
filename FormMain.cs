using System;
using System;
using System.Configuration;
using System.IO;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace Tools.Base64Creator
{
	public partial class FormMain : Form
	{
		public FormMain()
		{
			InitializeComponent();

            this.progressBar1.Maximum = 10000;
            this.progressBar1.Step = 1;

            this.labelResult.Text = "";

        }


        /// <summary>
        /// Создать объект хранилища
        /// </summary>
        public X509Store CreateStoreObject(string storeName, StoreLocation storeLocation)
		{
			StoreName res;
			var isSystemStore = Enum.TryParse(storeName, out res);
			if (isSystemStore)
				return new X509Store(res, storeLocation);
			return new X509Store(storeName, storeLocation);
		}

		private void FormMain_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
				foreach (string fileLoc in filePaths)
				{
					// Code to read the contents of the text file
                    string base64Path = fileLoc + ".base64.txt";
                    EncodeFromFile(fileLoc, base64Path, this.checkBoxSplit.Checked);
                    //try
                    //{
                    //    EncodeFromFile(fileLoc, base64Path);
                    //} catch(Exception ex)
                    //{
                    //    MessageBox.Show(ex.Message, "Ошибка");
                    //    labelResult.Text = DateTime.Now.ToLocalTime() + ":\n"
                    //        + ex.Message + "\n"
                    //        + ex.StackTrace;
                    //}
                    
				}
			}
		}

        // Read in the specified source file and write out an encoded target file.
        private void EncodeFromFile(string sourceFile, string targetFile, bool useSplit)
        {
            int splitSize = 0;
            int splitMaxSize = 64;
            this.progressBar1.Value = 0;
            // Verify members.cs exists at the specified directory.
            if (!File.Exists(sourceFile))
            {
                MessageBox.Show(
                "Unable to locate source file located at "
                + sourceFile + ".\n"
                + "Please correct the path and run the "
                + "sample again.", "Ошибка открытия файла");
                return;
            }

            // Retrieve the input and output file streams.
            using (FileStream inputFileStream =
                new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            {
                long fileSize = inputFileStream.Length;

                using (FileStream outputFileStream =
                    new FileStream(targetFile, FileMode.Create, FileAccess.Write))
                {
                    // Create a new ToBase64Transform object to convert to base 64.
                    ToBase64Transform base64Transform = new ToBase64Transform();

                    // Create a new byte array with the size of the output block size.
                    byte[] outputBytes = new byte[base64Transform.OutputBlockSize];

                    // Retrieve the file contents into a byte array.
                    //byte[] inputBytes = new byte[inputFileStream.Length];
                    //inputFileStream.Read(inputBytes, 0, inputBytes.Length);

                    // Verify that multiple blocks can not be transformed.
                    if (!base64Transform.CanTransformMultipleBlocks)
                    {
                        // Initializie the offset size.
                        int inputOffset = 0;
                        int outputOffset = 0;
                        int readBlockLenght = 0;
                        int base64BytesCount = 0;

                        // Iterate through inputBytes transforming by blockSize.
                        int inputBlockSize = base64Transform.InputBlockSize;
                        byte[] inputBytesBuffer = new byte[base64Transform.InputBlockSize];

                        while (inputFileStream.Length - inputOffset > inputBlockSize)
                        {
                            readBlockLenght = inputFileStream.Read(inputBytesBuffer, 0, inputBytesBuffer.Length);
                            base64BytesCount = base64Transform.TransformBlock(
                                inputBytesBuffer,
                                0,
                                readBlockLenght,
                                outputBytes,
                                0);

                            inputOffset += base64Transform.InputBlockSize;

                            outputFileStream.Write(
                                outputBytes,
                                0,
                                base64BytesCount);
                            outputOffset += base64BytesCount;

                            if (useSplit)
                            {
                                splitSize += base64BytesCount;
                                if (splitSize >= splitMaxSize)
                                {
                                    byte[] delim = { 0x0D, 0x0A };

                                    outputFileStream.Write(
                                        delim,
                                        0,
                                        2);
                                    splitSize = 0;
                                }
                            }

                            double realPosition = (1.0 * inputOffset) / fileSize;
                            double progressPosition = (1.0 * this.progressBar1.Value) / this.progressBar1.Maximum;
                            while (progressPosition < realPosition)
                            {
                                this.progressBar1.PerformStep();
                                progressPosition = (1.0 * this.progressBar1.Value) / this.progressBar1.Maximum;
                            }


                        }

                        readBlockLenght = inputFileStream.Read(inputBytesBuffer, 0, inputBytesBuffer.Length);
                        // Transform the final block of data.
                        outputBytes = base64Transform.TransformFinalBlock(
                            inputBytesBuffer,
                            0,
                            readBlockLenght);

                        outputFileStream.Write(outputBytes, 0, outputBytes.Length);
                        this.labelResult.Text = DateTime.Now.ToLocalTime() + ": " 
                            + "Created encoded file at " + targetFile;

                    }

                    // Determine if the current transform can be reused.
                    if (!base64Transform.CanReuseTransform)
                    {
                        // Free up any used resources.
                        base64Transform.Clear();
                    }

                    outputFileStream.Flush();

                    // Close file streams.
                    inputFileStream.Close();
                    outputFileStream.Close();
                }
            }
                
        }

		private void FormMain_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}
	}
}
