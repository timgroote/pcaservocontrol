using System.Threading.Channels;

namespace Robocontrol
{
    public class Program
    {
        public static List<IServoController> Servos = new();

        public static List<Task> MaintenanceTasks = new();

        public static PCA9685 pca;

        public static async Task Main(string[] args)
        {
            bool mock = args.Length == 0 || args[0] != "live";

            if(!mock)
            {
                pca = new PCA9685(0x40, false);
                pca.setPWMFreq(50);
            }

            for(int i =0; i < 3; i++)
            {
                if (mock)
                {
                    Servos.Add(new MockServoController((byte)i, i, 0, 180, 0));
                }
                else
                {
                    Servos.Add(new PCA9685ServoController(pca, (byte)i, i, 0, 180, 0));
                }
            }


            Console.WriteLine("Servos configured, enter a servo ID followed by the target angle or 'exit'");


            var channel = Channel.CreateUnbounded<ServoControlMessage>();

            foreach (var s in Servos)
            {
                MaintenanceTasks.Add(MaintenanceTaskAsync(channel.Reader, s));
            }

            string msg;

            do
            {
                Console.WriteLine("Enter command (index targetAngle) or 'exit'");
                msg = Console.ReadLine();

                if (msg == null)
                    continue; 

                var msgComponents = msg.Split(' ');

                try
                {
                    if (msgComponents.Length > 1)
                    {
                        var id = int.Parse(msgComponents[0]);

                        if(!Servos.Any(s => s.Id == id))
                        {
                            Console.Error.WriteLine("No such servo id : {0}", id);
                        }
                        else
                        { 
                            var ang = Double.Parse(msgComponents[1]);
                            await channel.Writer.WriteAsync(new ServoControlMessage() { ServoIdentifier = id, TargetAngle = ang });
                        }
                    }
                }
                catch (Exception e) {
                    Console.Error.WriteLine("Unable to handle input");
                }
            }
            while (msg != "exit");


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("exit requested. halting servo control threads.");

            foreach(var s in Servos)
            {
                await channel.Writer.WriteAsync(new ServoControlMessage() { ServoIdentifier = s.Id, ShutDown = true, TargetAngle = 0 });
            }

            Task.WaitAll(MaintenanceTasks.ToArray());

            Console.WriteLine("All servo maintenance tasks have halted. goodbye!");

            Console.ReadLine();

        }

        private static async Task MaintenanceTaskAsync(ChannelReader<ServoControlMessage> reader, IServoController servo)
        {
            servo.Init();

            while(true)
            {
                if (reader.TryPeek(out ServoControlMessage svMsg))
                {
                    if (svMsg?.ServoIdentifier == servo.Id)
                    {
                        Console.ForegroundColor = (ConsoleColor) (9 + servo.Id);
                        Console.WriteLine("Servo msg : {0} : {1}", svMsg.ServoIdentifier ,(svMsg.ShutDown ? "shutdown " : ("To " + svMsg.TargetAngle)));

                        var m = await reader.ReadAsync();
                        if(m.ShutDown)
                        {
                            break;
                        }
                        servo.SetTargetAngle(m.TargetAngle);
                    }
                }
                await Task.Delay(10); //10 ms delay? is this any use? probably garbage, need to find out
                servo.Tick();
            }

            Console.WriteLine("Halting {0}", servo.Id);
        }
    }

    public class ServoControlMessage()
    {
        public int ServoIdentifier { get; set; }
        public double TargetAngle { get; set; }

        public bool ShutDown { get; set; }

    }
}
