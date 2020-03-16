using System;
using System.Collections.Generic;
using System.IO;

public class Point3d
{
    private double[] coordinates;
    public double this[int index]
    {
        get
        {
            return coordinates[index];
        }
    }
    public Point3d(double x, double y, double z)
    {
        coordinates = new double[3];
        coordinates[0] = x;
        coordinates[1] = y;
        coordinates[2] = z;
    }
}

public class Point2d
{
    private int[] position;
    public int this[int index]
    {
        get
        {
            return position[index];
        }
    }
    public Point2d(int i, int j)
    {
        position = new int[2];
        position[0] = i;
        position[1] = j;
    }
}

public class Trajectory
{
    private List<Point3d> trajectory3d;

    public Trajectory()
    {
        trajectory3d = new List<Point3d>();
    }

    public void BuildTrace3dFromFile(string filePath)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("File dose not exist!");
            Console.ResetColor();
            return;
        }
        using (StreamReader streamReader = new StreamReader(filePath, System.Text.Encoding.Default))
        {
            while (!streamReader.EndOfStream)
            {
                string[] coordinate = streamReader.ReadLine().Split(',');
                double x = double.Parse(coordinate[0]);
                double y = double.Parse(coordinate[1]);
                double z = double.Parse(coordinate[2]);
                trajectory3d.Add(new Point3d(x, y, z));
            }
        }
    }

    public char[,] BuildMatrix(double step, int dimension1, int dimension2)
    {
        double maxDimension1 = trajectory3d[0][dimension1];
        double minDimension1 = trajectory3d[0][dimension1];
        double maxDimension2 = trajectory3d[0][dimension2];
        double minDimension2 = trajectory3d[0][dimension2];
        for (int i = 1; i < trajectory3d.Count; i++)
        {
            maxDimension1 = (trajectory3d[i][dimension1] > maxDimension1) ? trajectory3d[i][dimension1] : maxDimension1;
            minDimension1 = (trajectory3d[i][dimension1] < minDimension1) ? trajectory3d[i][dimension1] : minDimension1;
            maxDimension2 = (trajectory3d[i][dimension2] > maxDimension2) ? trajectory3d[i][dimension2] : maxDimension2;
            minDimension2 = (trajectory3d[i][dimension2] < minDimension2) ? trajectory3d[i][dimension2] : minDimension2;
        }
        int stepNumber1 = (int)Math.Floor((maxDimension1 - minDimension1) / step) + 1;
        int stepNumber2 = (int)Math.Floor((maxDimension2 - minDimension2) / step) + 1;
        char[,] matrix2d = new char[stepNumber1 + 40, stepNumber2 + 40];

        int originPosition1 = (int)Math.Floor((trajectory3d[0][dimension1] - minDimension1) / step) + 1;
        int originPosition2 = (int)Math.Floor((trajectory3d[0][dimension2] - minDimension2) / step) + 1;

        List<Point2d> trajectory2d = new List<Point2d>();
        for (int i = 0; i < trajectory3d.Count; i++)
        {
            int position1 = originPosition1 + (int)Math.Floor((trajectory3d[i][dimension1] - trajectory3d[0][dimension1]) / step);
            int position2 = originPosition2 + (int)Math.Floor((trajectory3d[i][dimension2] - trajectory3d[0][dimension2]) / step);
            trajectory2d.Add(new Point2d(position1, position2));
        }
        FillPoints(matrix2d, trajectory2d);
        FindPassingGrids(matrix2d, trajectory2d, step);
        return matrix2d;
    }

    public void FillPoints(char[,] matrix2d, List<Point2d> trajectory2d)
    {
        for (int i = 0; i < trajectory3d.Count; i++)
        {
            matrix2d[trajectory2d[i][0], trajectory2d[i][1]] = '+';
            string text = new string(" ");
            for (int j = 0; j < 3; j++)
            {
                text += Math.Round(trajectory3d[i][j], 1).ToString("F1");
                text += ' ';
            }
            for (int j = 0; j < text.Length; j++)
            {
                matrix2d[trajectory2d[i][0], trajectory2d[i][1] + 1 + j] = text[j];
            }
        }
        matrix2d[trajectory2d[0][0], trajectory2d[0][1]] = '=';
        matrix2d[trajectory2d[trajectory3d.Count - 1][0], trajectory2d[trajectory3d.Count - 1][1]] = '#';
    }

    public void OutputMatrix(char[,] matrix2d, string filename)
    {
        string filePath = filename;
        FileInfo fileInfo = new FileInfo(filePath);
        if (!fileInfo.Directory.Exists)
        {
            fileInfo.Create();
        }
        using (StreamWriter streamWriter = new StreamWriter(filePath, false, System.Text.Encoding.Default))
        {
            for (int j = 0; j < matrix2d.GetLength(1); j++)
            {
                for (int i = 0; i < matrix2d.GetLength(0); i++)
                {
                    streamWriter.Write((matrix2d[i, j] is '\0') ? " " : matrix2d[i, j].ToString());
                }
                streamWriter.Write('\n');
            }
        }
    }

    public void FindPassingGrids(char[,] matrix2d, List<Point2d> trajectory2d, double step)
    {
        for (int i = 1; i < trajectory2d.Count; i++)
        {
            if (trajectory2d[i][0] == trajectory2d[i - 1][0] && trajectory2d[i][1] == trajectory2d[i - 1][1])
            {
                continue;
            }

            if (trajectory2d[i][0] == trajectory2d[i - 1][0])
            {
                int sign0 = (trajectory2d[i][1] > trajectory2d[i - 1][1]) ? 1 : -1;
                int j = trajectory2d[i - 1][1] + sign0;
                while (j != trajectory2d[i][1])
                {
                    matrix2d[trajectory2d[i - 1][0], j] = '|';
                    j += sign0;
                }
                continue;
            }

            List<int[]> passingGrids = new List<int[]>();
            passingGrids.Add(new int[] { trajectory2d[i - 1][0], trajectory2d[i - 1][1] });
            int sign1 = (trajectory2d[i][0] > trajectory2d[i - 1][0]) ? 1 : -1;
            int sign2 = (trajectory2d[i][1] > trajectory2d[i - 1][1]) ? 1 : -1;
            int[] sign = new int[] { sign1, sign2 };
            int[] newPosition = new int[2] { trajectory2d[i - 1][0], trajectory2d[i - 1][1] };
            double k = (double)(trajectory2d[i][1] - trajectory2d[i - 1][1]) / (double)(trajectory2d[i][0] - trajectory2d[i - 1][0]);
            int primaryDirection = 0;
            int secondaryDirection = 1;
            if (Math.Abs(k) >= 1)
            {
                primaryDirection = 1;
                secondaryDirection = 0;
                k = 1 / k;
            }
            double d = 0.5 * step * k;
            if (sign[secondaryDirection] < 0)
            {
                d = step - d;
            }
            int stepNumber = Math.Abs(trajectory2d[i][primaryDirection] - trajectory2d[i - 1][primaryDirection]) - 1;
            while (stepNumber > 0)
            {
                stepNumber -= 1;
                newPosition[primaryDirection] += sign[primaryDirection];
                d += step * Math.Abs(k);
                if (d >= step)
                {
                    d -= step;
                    newPosition[secondaryDirection] += sign[secondaryDirection];
                }
                passingGrids.Add(new int[] { newPosition[0], newPosition[1] });
            }
            passingGrids.Add(new int[] { trajectory2d[i][0], trajectory2d[i][1] });
            FillPath(matrix2d, passingGrids);
        }
    }

    public void FillPath(char[,] matrix2d, List<int[]> passingGrids)
    {
        int directionSign1 = (passingGrids[passingGrids.Count - 1][0] > passingGrids[0][0]) ? 1 : -1;
        int directionSign2 = (passingGrids[passingGrids.Count - 1][1] > passingGrids[0][1]) ? 1 : -1;
        for (int j = 1; j < passingGrids.Count - 1; j++)
        {
            if (matrix2d[passingGrids[j][0], passingGrids[j][1]] != '\0')
            {
                continue;
            }
            if (passingGrids[j][0] == passingGrids[j + 1][0])
            {
                matrix2d[passingGrids[j][0], passingGrids[j][1]] = '|';
                continue;
            }
            else if (passingGrids[j][1] == passingGrids[j + 1][1])
            {
                matrix2d[passingGrids[j][0], passingGrids[j][1]] = '-';
                continue;
            }
            else if (directionSign1 == directionSign2)
            {
                matrix2d[passingGrids[j][0], passingGrids[j][1]] = '\\';
                continue;
            }
            else
            {
                matrix2d[passingGrids[j][0], passingGrids[j][1]] = '/';
                continue;
            }
        }
    }

    public void ViewVertical(double step, string filename)
    {
        char[,] matrix2d = BuildMatrix(step, 0, 1);
        OutputMatrix(matrix2d, filename);
    }

    public void ViewFront(double step, string filename)
    {
        char[,] matrix2d = BuildMatrix(step, 0, 2);
        OutputMatrix(matrix2d, filename);
    }

    public void ViewSide(double step, string filename)
    {
        char[,] matrix2d = BuildMatrix(step, 1, 2);
        OutputMatrix(matrix2d, filename);
    }

    public static void Main()
    {
        Trajectory test = new Trajectory();
        test.BuildTrace3dFromFile("3.txt");
        test.ViewVertical(5, "Vertical.txt");
        test.ViewFront(5, "Front.txt");
        test.ViewSide(5, "Side.txt");
    }
}
