using OpenScrape.App.Helpers;

namespace OpenScrape.App.Aplication.UseCases
{
    public class SaveTableMapUseCase : ISaveTableMapUseCase
    {

        public void Execute(SaveTableMapUseCaseRequest request)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = @"C:\Code\ScrapePoker\resources\Games";
            saveFileDialog.Filter = "Text|*.txt";
            saveFileDialog.Title = "Save an Text File";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {

                if (saveFileDialog.FileName != "")
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.OpenFile()))
                    {
                        writer.WriteLine();
                        writer.WriteLine("//");
                        writer.WriteLine("// Regions");
                        writer.WriteLine("//");
                        writer.WriteLine();

                        foreach (var item in request.Regions)
                        {
                            if (!string.IsNullOrEmpty(item.Name))
                                writer.WriteLine($"r${item.Name} # {item.X} - {item.Y} - {item.Width} - {item.Height} & {item.IsHash} & {item.IsColor} & {item.IsBoard} & {item.Color}");
                        }

                        writer.WriteLine();
                        writer.WriteLine("//");
                        writer.WriteLine("// Board");
                        writer.WriteLine("//");
                        writer.WriteLine();

                        foreach (var item in request.Board)
                        {
                            if (item.Image != null)
                            {
                                string imgText = EncrypterHelper.GetImageEncrypted(item.Image, request.Key);
                                writer.WriteLine($"b${item.Name} - {imgText}");
                            }
                        }

                        writer.WriteLine();
                        writer.WriteLine("//");
                        writer.WriteLine("// Images");
                        writer.WriteLine("//");
                        writer.WriteLine();

                        foreach (var item in request.Images)
                        {
                            if (item.Image != null)
                            {
                                string imgText = EncrypterHelper.GetImageEncrypted(item.Image, request.Key);
                                writer.WriteLine($"i${item.Name} - {imgText}");
                            }
                        }

                        writer.Flush();
                        writer.Close();
                    }
                }

            }
        }




    }
}
