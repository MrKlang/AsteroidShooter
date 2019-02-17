using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    [Serializable]
    public class AsteroidsData
    {
        public float[,] AsteroidXDirection;
        public float[,] AsteroidYDirection;
        public float[,] AsteroidSpeed;
    }
}
