﻿using System;
using System.Collections.Generic;
using System.IO;

public class InOutOperations
{
    public static void BuildTrace3dFromFile(int number, List<Point3d> trajectory3d)
    {
        string fileName = number.ToString() + ".csv";
        FileInfo fileInfo = new FileInfo(fileName);
        if (!fileInfo.Exists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("File dose not exist!");
            Console.ResetColor();
            return;
        }
        using (StreamReader streamReader = new StreamReader(fileName, System.Text.Encoding.Default))
        {
            while (!streamReader.EndOfStream)
            {
                string[] coordinate = streamReader.ReadLine().Split(','); // data reading error, not required format
                double x = double.Parse(coordinate[0]);
                double y = double.Parse(coordinate[1]);
                double z = double.Parse(coordinate[2]);
                trajectory3d.Add(new Point3d(x, y, z));
            }
        }
    }
    public static void OutputMatrix(char[,] matrix2d, string filename)
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
}
public class Trajectory
{
    // private List<Point3d> trajectory3d;

    // public Trajectory()
    // {
    //     trajectory3d = new List<Point3d>();
    // }
    public static char[,] BuildMatrix(double step, int dimension1, int dimension2, string unitOption, List<Point3d> trajectory3d)
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
            int position1 = CalculatePosition(originPosition1, i, trajectory3d, step, dimension1);
            int position2 = CalculatePosition(originPosition2, i, trajectory3d, step, dimension2);
            trajectory2d.Add(new Point2d(position1, position2));
        }
        FillPoints(matrix2d, trajectory2d, unitOption, trajectory3d);
        FindPassingGrids(matrix2d, trajectory2d, step);
        return matrix2d;
    }
    public static int CalculatePosition(int originPosition, int i, List<Point3d> trajectory3d, double step, int dimension)
    {
        return originPosition + (int)Math.Floor((trajectory3d[i][dimension] - trajectory3d[0][dimension]) / step);
    }
    public static void FillPoints(char[,] matrix2d, List<Point2d> trajectory2d, string unitOption, List<Point3d> trajectory3d)
    {
        const double meterToFeetRatio = 3.2808399;
        double ratio = unitOption.Equals("feet") ? meterToFeetRatio : 1.0;
        for (int i = 0; i < trajectory3d.Count; i++)
        {
            matrix2d[trajectory2d[i][0], trajectory2d[i][1]] = '+';
            string text = new string("");
            for (int j = 0; j < 3; j++)
            {
                text += Math.Round(trajectory3d[i][j] * ratio, 1).ToString("F1");
                text += (j == 2) ? "" : ",";
            }
            matrix2d[trajectory2d[i][0] + 1, trajectory2d[i][1] + 1] = '(';
            for (int j = 0; j < text.Length; j++)  // if text is out of border of figure
            {
                matrix2d[trajectory2d[i][0] + j + 2, trajectory2d[i][1] + 1] = text[j];
            }
            matrix2d[trajectory2d[i][0] + text.Length + 2, trajectory2d[i][1] + 1] = ')';
        }
        matrix2d[trajectory2d[0][0], trajectory2d[0][1]] = '=';
        matrix2d[trajectory2d[trajectory3d.Count - 1][0], trajectory2d[trajectory3d.Count - 1][1]] = '#';
        SpotInflectionPoint(matrix2d, trajectory2d, trajectory3d);
    }
    public static void SpotInflectionPoint(char[,] matrix2d, List<Point2d> trajectory2d, List<Point3d> trajectory3d)
    {
        int maxCosineIndex = 0;//calculate sharpest inflection point
        double maxCosine = -1.0;
        double vector1X, vector1Y, vector2X, vector2Y, vector1Z, vector2Z;
        for (int i = 1; i < trajectory3d.Count - 1; i++)
        {
            vector1X = trajectory3d[i - 1][0] - trajectory3d[i][0];
            vector1Y = trajectory3d[i - 1][1] - trajectory3d[i][1];
            vector1Z = trajectory3d[i - 1][2] - trajectory3d[i][2];
            vector2X = trajectory3d[i + 1][0] - trajectory3d[i][0];
            vector2Y = trajectory3d[i + 1][1] - trajectory3d[i][1];
            vector2Z = trajectory3d[i + 1][2] - trajectory3d[i][2];
            double cosine = (vector1X * vector2X + vector1Y * vector2Y + vector1Z * vector2Z) / (Math.Sqrt(vector1X * vector1X + vector1Y * vector1Y + vector1Z * vector1Z) * Math.Sqrt(vector2X * vector2X + vector2Y * vector2Y + vector2Z * vector2Z));
            if (maxCosine < cosine)
            {
                maxCosineIndex = i; maxCosine = cosine;
            }
        }
        matrix2d[trajectory2d[maxCosineIndex][0], trajectory2d[maxCosineIndex][1]] = 'o';
    }
    public static void FindPassingGrids(char[,] matrix2d, List<Point2d> trajectory2d, double step)
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
            double d = 0.5 * step + 0.5 * step * k;
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

    public static void FillPath(char[,] matrix2d, List<int[]> passingGrids)
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
}
public class ViewSetup
{
    public static void ViewVertical(double step, int number, string unitOption, List<Point3d> trajectory3d)
    {
        char[,] matrix2d = Trajectory.BuildMatrix(step, 0, 1, unitOption, trajectory3d);
        InOutOperations.OutputMatrix(matrix2d, number.ToString() + "Vertical.txt");
    }

    public static void ViewFront(double step, int number, string unitOption, List<Point3d> trajectory3d)
    {
        char[,] matrix2d = Trajectory.BuildMatrix(step, 0, 2, unitOption, trajectory3d);
        InOutOperations.OutputMatrix(matrix2d, number.ToString() + "Front.txt");
    }

    public static void ViewSide(double step, int number, string unitOption, List<Point3d> trajectory3d)
    {
        char[,] matrix2d = Trajectory.BuildMatrix(step, 1, 2, unitOption, trajectory3d);
        InOutOperations.OutputMatrix(matrix2d, number.ToString() + "Side.txt");
    }

    public static void View(double step, int number, string unitOption, List<Point3d> trajectory3d)
    {
        ViewVertical(step, number, unitOption, trajectory3d);
        ViewFront(step, number, unitOption, trajectory3d);
        ViewSide(step, number, unitOption, trajectory3d);
    }
}
public class Program
{
    public static void Main()
    {
        List<Point3d> trajectory3d = new List<Point3d>();
        int number = 5;
        InOutOperations.BuildTrace3dFromFile(number, trajectory3d);
        Console.WriteLine("Please enter display unit(default meter): meter/feet");
        string unitOption = Console.ReadLine();
        ViewSetup.View(3.28, number, unitOption, trajectory3d);
    }
}