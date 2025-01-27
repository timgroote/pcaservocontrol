namespace Robocontrol
{
    public class PCA9685ServoController : IServoController
    {
        public PCA9685 Pca { get; }
        public byte Channel { get; private set; }
        public double MinAngle { get; private set; }
        public double MaxAngle { get; private set; }
        public double AngleRange { get; private set; }

        public double CurrentAngle { get; private set; }

        /// <summary>
        /// an optional offset angle for motors that are mounted in an offset angle
        /// </summary>
        public double MountedOffset { get; }
        public double TargetAngle { get; private set; }

        public int Id { get; set; }
        private bool Dirty { get; set; }

        public PCA9685ServoController(PCA9685 pca, byte channel, int id, double minAngle, double maxAngle, double mountedOffsetAngle)
        {
            Id = id;
            Pca = pca;
            Channel = channel;
            MinAngle = minAngle;
            MaxAngle = maxAngle;
            AngleRange = maxAngle - minAngle;
            MountedOffset = mountedOffsetAngle;
        }

        public void Init()
        {
            Console.WriteLine("Init servo {0}", Id);
        }

        public void SetTargetAngle(double degrees)
        {
            double clampedAng = Math.Min(MaxAngle, Math.Max(MinAngle, degrees - MountedOffset));
            if (TargetAngle != clampedAng)
            {
                TargetAngle = clampedAng;
                Dirty = true;
            }
        }

        public void Tick()
        {
            if (CurrentAngle < TargetAngle)
            {
                CurrentAngle += (TargetAngle - CurrentAngle) * 0.1;
            }

            if (!Dirty)
                return;

            try
            {
                SetAngleOnControlBoard();
            }
            catch { }
            finally { Dirty = false; }

        }

        private const int minPulse = 500;
        private const int pulseRange = 2000;

        private void SetAngleOnControlBoard()
        {
            Console.WriteLine("-EXEC-> sending angle {0}", TargetAngle);

            var clampedAngle = Math.Clamp(TargetAngle, MinAngle, MaxAngle);
            if(clampedAngle != TargetAngle)
            {
                Console.WriteLine("-WARN-> angle exceeds min/max and is clamped");
            }

            Pca.setServoPulse(Channel, (int)Math.Round(minPulse + ((TargetAngle / 180.0) * pulseRange)));
        }
    }
}
