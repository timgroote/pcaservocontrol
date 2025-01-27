namespace Robocontrol
{
    public class ServoController : IServoController
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Address">Intended for i2c address, figure out how</param>
        /// <param name="minAngle">minimum angle that can be requested of the servo</param>
        /// <param name="maxAngle">maximum angle that can be requested of the servo</param>
        public ServoController(object Address, int id, double minAngle, double maxAngle, double mountedOffsetAngle)
        {
            Id = id;
            this.Address = Address;
            MinAngle = minAngle;
            MaxAngle = maxAngle;
            MountedOffset = mountedOffsetAngle;
        }

        public object Address { get; private set; }
        public double MinAngle { get; private set; }
        public double MaxAngle { get; private set; }

        /// <summary>
        /// an optional offset angle for motors that are mounted in an offset angle
        /// </summary>
        public double MountedOffset { get; }
        public double TargetAngle { get; private set; }
        public int Id { get; set; }
        private bool Dirty { get; set; }

        public void Init()
        {
            Console.WriteLine("Init servo {0}", Id);
        }

        public void SetTargetAngle(double degrees)
        {
            double clampedAng = Math.Min(MaxAngle, Math.Max(MinAngle, degrees - MountedOffset));
            if(TargetAngle != clampedAng)
            { 
                TargetAngle = clampedAng;
                Dirty = true;
            }
        }

        public void Tick()
        {
            if (!Dirty)
                return;

            try
            { 
                SetAngleOnControlBoard();
            }
            catch { }
            finally { Dirty = false; }

        }

        private void SetAngleOnControlBoard()
        {
            Console.WriteLine("(pretend) sending angle {0}", TargetAngle);
            //throw new NotImplementedException();
        }
    }
}
