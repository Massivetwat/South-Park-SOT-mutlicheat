using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SOTTRAINER
{
    public class entity
    {
        public IntPtr baseAddress;
        public string? name;
        public float currentHealth, maxHealth, currentStamina, maxStamina;
        public Vector3 pos;

    }

    public class viewmatrix
    {
        public float

            m11, m12, m13, m14,
            m21, m22, m23, m24,
            m31, m32, m33, m34,
            m41, m42, m43, m44;


    }
}
