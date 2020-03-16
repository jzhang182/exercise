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