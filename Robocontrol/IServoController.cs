namespace Robocontrol
{
    public interface IServoController
    {
        public int Id { get; set; }

        public void Init();

        public void SetTargetAngle(double degrees);

        public void Tick();
    }
}
