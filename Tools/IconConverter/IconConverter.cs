using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: IconConverter <input.png> <output.ico>");
            return;
        }

        string inputPath = args[0];
        string outputPath = args[1];

        byte[] pngData = File.ReadAllBytes(inputPath);
        
        using (FileStream fs = new FileStream(outputPath, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            // ICONDIR structure
            writer.Write((short)0);      // Reserved
            writer.Write((short)1);      // Type (1=Icon)
            writer.Write((short)1);      // Count (1 image)

            // ICONDIRENTRY structure
            writer.Write((byte)0);       // Width (0 = 256px)
            writer.Write((byte)0);       // Height (0 = 256px)
            writer.Write((byte)0);       // ColorCount (0 = No palette)
            writer.Write((byte)0);       // Reserved
            writer.Write((short)1);      // Planes
            writer.Write((short)32);     // BitCount
            writer.Write((int)pngData.Length); // BytesInRes
            writer.Write((int)22);       // ImageOffset (6 + 16 = 22)

            // PNG Data
            writer.Write(pngData);
        }

        Console.WriteLine("Converted successfully: " + outputPath);
    }
}
