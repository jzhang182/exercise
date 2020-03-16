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