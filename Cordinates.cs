using System;

namespace PWOOGFrameWork
{
    /// <summary>
    /// Класс, описывающий 3-мерную систему координат PW
    /// </summary>
    public class Point3
    {
        /// <summary>
        /// Возвращает угол между двумя точками
        /// </summary>
        /// <param name="p1">Точка 1</param>
        /// <param name="p2">Точка 2</param>
        /// <returns>Угол между точками</returns>
        public static double Angle(Point3 p1, Point3 p2)
        {
            float x = Math.Abs(p2.X - p1.X);
            float y = Math.Abs(p2.Y - p1.Y);
            return Math.Atan2(x, y) * (256 / 360);
        }

        /// <summary>
        /// Возвращает дистанцию между двумя точками
        /// </summary>
        /// <param name="p1">Точка 1</param>
        /// <param name="p2">Точка 2</param>
        /// <returns>Расстояние между двумя точками</returns>
        public static float Distance(Point3 p1, Point3 p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
        }

        private float x, y, z;

        /// <summary>
        /// Серверная координата X
        /// </summary>
        public float X { get { return x; } set { x = value; } }
        /// <summary>
        /// Серверная координата Y
        /// </summary>
        public float Y { get { return y; } set { y = value; } }
        /// <summary>
        /// Серверная координата Z
        /// </summary>
        public float Z { get { return z; } set { z = value; } }

        /// <summary>
        /// Игровая координата X
        /// </summary>
        public float GameX { get { return (x + 4000F) / 10F; } set { x = (value * 10F) - 4000F; } }
        /// <summary>
        /// Игровая координата Y
        /// </summary>
        public float GameY { get { return (y + 5500F) / 10F; } set { y = (value * 10F) - 5500F; } }
        /// <summary>
        /// Игровая координата Z
        /// </summary>
        public float GameZ { get { return z / 10F; } set { z = value * 10F; } }

        /// <summary>
        /// Объявляет класс координат
        /// </summary>
        /// <param name="x">Игровая координата X</param>
        /// <param name="y">Игровая координата Y</param>
        /// <param name="z">Игровая координата Z</param>
        public static Point3 FromGame(float x, float y, float z)
        {
            
            Point3 r = new Point3();
            r.GameX = x;
            r.GameY = y;
            r.GameZ = z;
            return r;
        }
        /// <summary>
        /// Объявляет класс координат
        /// </summary>
        /// <param name="x">Серверная координата X</param>
        /// <param name="y">Серверная координата Y</param>
        /// <param name="z">Серверная координата Z</param>
        public Point3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        private Point3() { }

        /// <summary>
        /// Сравнивает объекты на равенство
        /// </summary>
        /// <param name="obj">Объект для сравнения</param>
        /// <returns>Равны ли они</returns>
        public bool Equals(Point3 obj)
        {
            if (obj.x == x && obj.y == y && obj.z == z)
                return true;
            return false;
        }

        /// <summary>
        /// Создаёт копию объекта
        /// </summary>
        /// <returns>Копия объекта</returns>
        public Point3 Clone()
        {
            return new Point3(this.x, this.y, this.z);
        }
    }
}
