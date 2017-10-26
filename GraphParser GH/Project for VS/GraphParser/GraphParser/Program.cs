using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using tessnet2;
using System.IO;

namespace GraphParser
{
    class Program
    {
        static Point YAxisA; //начальная точка оси ординат
        static Point XAxisA; //начальная точка оси абсцисс

        static double firstNumAxisY; //верхнее число на оси ординат
        static double lastNumAxisY; //нижнее число на оси ординат

        static double digitsAfterZero = 10; //вспомогательная штука, когда числа не распознаны

        const int depth = 180;//цвет графика
        const int smallImgK = 4;//коэффициент растягивания для распознания чисел
        //tessnet не может обработать слишком маленькие картинки цифор, поступающих на вход

        const int borderTemp = 70;

        static void Main(string[] args)
        {
            Console.WriteLine("Программа считывает значения с графиков в формате .jpg");
            Console.WriteLine("Какой тип? TempDB - 1, TempDp24 - 2");

            bool isOk = false;
            while (!isOk)
            {
                string a = Console.ReadLine();
                if (a == "1")
                {
                    digitsAfterZero = Math.Pow(digitsAfterZero,1); 
                    isOk = true;
                }
                else if (a == "2")
                {
                    digitsAfterZero = Math.Pow(digitsAfterZero, 3);
                    isOk = true;
                }
                else
                    Console.WriteLine("Неверный формат");
            }
            Console.WriteLine("Введите путь к папке с графиками \n(если в \"./data/\", то оставить пустым):");
            string dataPath = "";
            isOk = false;
            while (!isOk)
            {
                string a = Console.ReadLine();

                if (a == "")
                {
                    dataPath = "./data/";
                    isOk = true;
                }
                else if (Directory.Exists(a))
                {
                    dataPath = a;
                    isOk = true;
                }
                else 
                    Console.WriteLine("Указанная директория отсутствует");

            }

            Console.WriteLine("Введите путь выходного файла \n(если в \"./output.txt\", то оставить пустым):");
            string outputPath = "";
            string path = Console.ReadLine();

            if (path == "")
                outputPath = "./output.txt";
            else 
                outputPath = path;

            string[] fileNames = Directory.GetFiles(dataPath);
            
            StreamWriter sr = new StreamWriter(outputPath);

            for (int l = 0; l < fileNames.Length; l++) //начинаем перебирать все файлы в папке
            {

                string filePath = fileNames[l];
                string date = filePath.Split('/').Last(); //разделяем строчку, чтобы достать дату
                date = date.Split('.')[0]; 

                
                try
                {
                    if (filePath.Split('.').Last() != "jpg") throw new FormatException(); 
                    

                    Bitmap sourceImage = new Bitmap(filePath); 

                    //1 find axis
                    YAxisA = FindAxisTop(CropImage(sourceImage, new Rectangle(0, 0, 70, 15)), depth); //ОБОБЩИТЬ
                    //находим точку оси, пошагово перебирая пиксели 

                    //2 Crop Image

                    Bitmap numbers = CropImage(sourceImage, new Rectangle(0, 0, YAxisA.X, sourceImage.Height)); 
                    Bitmap graph = CropImage(sourceImage, new Rectangle(YAxisA.X, 0, sourceImage.Width, sourceImage.Height));

                    //3 find number positions

                    Point[] numberPositions = FindDotsInVerticalLine(numbers, YAxisA.X - 2, depth);
                    //находим положения чисел на оси ординат, пошагово находя маленькие отрезки на оси

                    //4 make bitmap readable for tessnet

                    numbers = StrechImage(numbers, numbers.Width * 4, numbers.Height * 4);
                    //изображение с цифрами, иначе tessnet не может распознать


                    //5 tessnet this stuff
                    
                    var image = numbers;
                    var ocr = new Tesseract();
                    ocr.SetVariable("tessedit_char_whitelist", "0123456789."); 
                    ocr.Init(".\tessdata", "eng", true);
                    Point[] numberPositionsOld = new Point[numberPositions.Length];
                    numberPositions.CopyTo(numberPositionsOld, 0);
                    string str1 = "";
                    string str2 = "";
                    for (int i = 0; i < numberPositions.Length; i++)
                    {

                        numberPositions[i] = new Point(numberPositions[i].X * 4 - 8, numberPositions[i].Y * 4);//т.к. изображение было растянуто, то координаты смесились 

                        if ((i == 0) || (i == (numberPositions.Length - 1))) //нужны только крайние значиня на оси, т.к. остальные расчитываются
                        {
                            int x0 = 0,
                            y0 = numberPositions[i].Y - 16,
                            x1 = numberPositions[i].X,
                            y1 = numberPositions[i].Y + 24;
                            int w = x1 - x0;
                            int h = y1 - y0;

                            Rectangle r = new Rectangle(x0, y0, w, h); //прямоугольник с нужным числом внутри
                            var result = ocr.DoOCR(image, r); //процесс распознания

                            string str = "";
                            foreach (Word word in result)
                            {
                                str += word.Text;
                            }
                            if (i == 0) str1 = str; //запишем, как справочную информацию, строки первого
                            else str2 = str;// и последнего чисел на осях

                            if (str != "~") //этот символ на выходе из tessnet значит, что строка не была распознана

                                if (str[0] == '.')
                                //tessnet не распознавал минус нормально, только как точку, которая стояла на первом месте
                                //даже, если минус был добавлен в белый лист чаров
                                //поэтому точка преобразуется в минус вручную  
                                {
                                    str = str.Substring(1);
                                    str = str.Replace('.', ','); //в типе double нам нужны запятые, а не точки
                                    if (i == 0)
                                        firstNumAxisY = double.Parse(str) * -1;
                                    else
                                        lastNumAxisY = double.Parse(str) * -1;

                                }
                                else
                                {
                                    str = str.Replace('.', ',');
                                    if (i == 0)
                                        firstNumAxisY = double.Parse(str);
                                    else
                                        lastNumAxisY = double.Parse(str);
                                }

                            else if (i == 0) 
                            {
                                Console.WriteLine("Файл: {0}", filePath);
                                Console.WriteLine("Не удалось распознать верхнее число на оси Y ({0})", str1);
                                
                                Console.WriteLine("Пожалуйста, введите значение, которое есть на графике вверху");
                                str = Console.ReadLine().Replace('.', ',');
                                firstNumAxisY = double.Parse(str);
                            }
                            else
                            {
                                Console.WriteLine("Файл: {0}", filePath);
                                Console.WriteLine("Не удалось распознать нижнее число на оси Y ({0})", str2);
                                
                                Console.WriteLine("Пожалуйста, введите значение, которое есть на графике внизу");
                                str = Console.ReadLine().Replace('.', ',');
                                lastNumAxisY = double.Parse(str);
                            }
                        }


                    }
                    string inp;
                    if ((firstNumAxisY > borderTemp) || (firstNumAxisY < -borderTemp)) 
                        //константа - температура которая не может быть достигнута
                        //следовательно значение неверное и его надо переспросить
                       
                    {
                        Console.WriteLine("Файл: {0}", filePath);
                        Console.WriteLine("Распознание произошло с низкой точнотью");
                        
                        Console.WriteLine("Это корректное значение верхнего числа на оси Y? \n(оставить пустым, если да, ввести новое, если нет)");
                        firstNumAxisY = firstNumAxisY / digitsAfterZero; //обычно теряется точка в середине числа
                        //поэтому программа сама предлагает верное значение
                        Console.WriteLine("{0} ({1})", firstNumAxisY, str1);
                        inp = "";
                        inp = Console.ReadLine();
                        inp = inp.Replace('.', ',');
                        if (inp != "") firstNumAxisY = double.Parse(inp);
                    }

                    if ((lastNumAxisY > borderTemp) || (lastNumAxisY < -borderTemp))
                    {
                        Console.WriteLine("Файл: {0}", filePath);
                        Console.WriteLine("Распознание произошло с низкой точнотью");
                        
                        Console.WriteLine("Это корректное значение нижнего числа на оси Y? \n(оставить пустым, если да, ввести новое, если нет)");
                        lastNumAxisY = lastNumAxisY / digitsAfterZero;
                        Console.WriteLine("{0} ({1})", lastNumAxisY, str2);
                        inp = "";
                        inp = Console.ReadLine();
                        inp = inp.Replace('.', ',');
                        if (inp != "") lastNumAxisY = double.Parse(inp);

                    }


                    //6 find the horizontal axis 


                    Bitmap b = CropImage(graph, new Rectangle(0, graph.Height - 25, 85, 24)); //ОБОБЩИТЬ
                    XAxisA = FindAxisBottom(b, depth); //ищем горизонтальную ось

                    XAxisA.Y += graph.Height - 25; 

                    //7 crop graph and numbers 

                    Bitmap times = CropImage(graph, new Rectangle(0, XAxisA.Y, graph.Width, graph.Height - XAxisA.Y));
                    graph = CropImage(graph, new Rectangle(0, 0, graph.Width, XAxisA.Y - 1));

                    //8 find the stepdots

                    Point[] valuesPositions = FindDotsInHorizontalLine(times, 1, depth);
                    //найдены отрезочки на оси абсцисс

                    //9 find the values on graph 

                    List<Point> graphValues = new List<Point>(); //список для значений кординат с графика
                    int currStep;

                    for (int k = 0; k < valuesPositions.Length; k++)
                    {
                        currStep = valuesPositions[k].X;
                        for (int i = 0; i < graph.Height; i++)
                        {
                            Color pix = graph.GetPixel(currStep, i);
                            if ((pix.R < depth) && (pix.B < depth) && (pix.G < depth)) //находим темную точку и заносим ее в список
                            {
                                graphValues.Add(new Point(currStep, i));
                                break;
                            }
                            graph.SetPixel(currStep, i, Color.Red);

                        }

                    }

                    //10 make values from cordinates

                    List<string> stringValues = new List<string>();

                    int endAxis = FindLastDot(sourceImage, YAxisA.X, depth).Y;
                    //крайняя точка оси ординат
                    int wholeAxis = (numberPositionsOld[numberPositionsOld.Length - 1].Y - numberPositionsOld[0].Y);
                    //длина отрезка между первым и последним числом в пикселях

                    double scale = firstNumAxisY - lastNumAxisY; //числовая мощность оси
                    for (int i = 0; i < 47; i++)
                    {
                        int hor = i;

                        int min = 0;
                        if ((hor % 2) != 0) min = 30;
                        hor = hor / 2; //формулы по преобразованию из кординат в значения с графика, доступные для человеческого понимания
                        double div = Convert.ToDouble(graphValues[i].Y - numberPositionsOld[0].Y) / wholeAxis;
                        double vert = firstNumAxisY - (div * scale + lastNumAxisY) + lastNumAxisY;

                        //stringValues.Add(string.Format("{3}\t{0}:{1:00}\t{2:0.00}\t{4};{5}", hor, min, vert,date, YAxisA.X, YAxisA.Y));
                        sr.WriteLine(string.Format("{3}\t{0}:{1:00}\t{2:0.00}", hor, min, vert, date));
                        Console.WriteLine(string.Format("{3}\t{0}:{1:00}\t{2:0.00}\t{4}:{5}\t\"{6}\" \"{7}\"", hor, min, vert, filePath, firstNumAxisY, lastNumAxisY, str1, str2));
                    }
                }

                catch (FormatException fex)
                {
                    Console.WriteLine("Неверный формат файла: {0}\nОбъект:{1}\nСообщение: {2}", filePath, fex.Source, fex.Message);

                }

                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при обработке файла: {0}\nОбъект:{1}\nСообщение: {2}", filePath, ex.Source ,ex.Message);
                }

            }
            sr.Close();
            Console.WriteLine("Finished");
            Console.ReadLine();
        }


        public static Point FindAxisBottom(Bitmap source, int depth)
        {

            for (int i = 0; i < source.Width; i++)
                for (int j = source.Height - 1; j != 0; j--)
                {
                    int r = source.GetPixel(i, j).R;
                    int g = source.GetPixel(i, j).G;
                    int b = source.GetPixel(i, j).B;
                    if ((r < depth) && (g < depth) && (b < depth)) return new Point(i + 1, j + 1);
                }

            return new Point();

        }


        public static Bitmap CropImage(Bitmap source, Rectangle section)
        {
            // Метод обрезки изображения
            Bitmap bmp = new Bitmap(section.Width, section.Height);

            Graphics g = Graphics.FromImage(bmp);


            g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);

            return bmp;
        }

        public static Bitmap StrechImage(Bitmap source, int width, int height)
        {
            Bitmap result = new Bitmap(source, width, height);
            return result;
        }

        public static Point FindAxisTop(Bitmap source, int depth)
        {
            
            for (int i = source.Width - 1; i > 0; i--)
                for (int j = 0; j < source.Height; j++)
                {
                    int r = source.GetPixel(i, j).R;
                    int g = source.GetPixel(i, j).G;
                    int b = source.GetPixel(i, j).B;
                    if ((r < depth) && (g < depth) && (b < depth)) return new Point(i + 1, j + 1);
                }
            return new Point();
        }
        public static Point[] FindDotsInVerticalLine(Bitmap source, int x, int depth)
        {
            List<Point> result = new List<Point>();
            for(int i = 0; i<source.Height; i++)
            {
                int r = source.GetPixel(x, i).R;
                int g = source.GetPixel(x, i).G;
                int b = source.GetPixel(x, i).B;
                if ((r < depth) && (g < depth) && (b < depth)) result.Add(new Point(x, i));
            }
            return result.ToArray();
        }
        public static Point[] FindDotsInHorizontalLine(Bitmap source, int y, int depth)
        {
            List<Point> result = new List<Point>();
            for (int i = 0; i < source.Width; i++)
            { 
                int r = source.GetPixel(i, y).R;
                int g = source.GetPixel(i, y).G;
                int b = source.GetPixel(i, y).B;
                if ((r < depth) && (g < depth) && (b < depth)) result.Add(new Point(i, y));
            }
            return result.ToArray();
        }

        public static Point FindLastDot(Bitmap source, int x, int depth)
        {
            x = x - 1;
            for (int i = source.Height - 1; i > 0; i--)
            {
                int r = source.GetPixel(x, i).R;
                int g = source.GetPixel(x, i).G;
                int b = source.GetPixel(x, i).B;
                if ((r < depth) && (g < depth) && (b < depth)) return new Point(x, i);
            }
            return new Point();
        }

    }
}
