using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace Robocontrol
{
    public static class PCADemo
    {
        //example that makes a servo wave just like in the python demo
        public static void Go()
        {
            var pwm = new PCA9685(0x40, false);
            pwm.setPWMFreq(50);
            while (true)
            {
                // setServoPulse(2,2500)
                for (int i = 500; i < 2500; i += 10)
                {
                    pwm.setServoPulse(0, i);
                    Thread.Sleep(2);
                }

                for (int i = 2500; i > 500; i -= 10)
                {
                    pwm.setServoPulse(0, i);
                    Thread.Sleep(2);
                }
            }
        }
    }

    public class PCA9685
    {
        public const byte SUBADR1 = 0x02;
        public const byte SUBADR2 = 0x03;
        public const byte SUBADR3 = 0x04;

        public const byte MODE1 = 0x00;

        public const byte PRESCALE = 0xFE;

        public const byte LED0_ON_L = 0x06;
        public const byte LED0_ON_H = 0x07;
        public const byte LED0_OFF_L = 0x08;
        public const byte LED0_OFF_H = 0x09;

        public const byte ALLED_ON_L = 0xFA;
        public const byte ALLED_ON_H = 0xFB;
        public const byte ALLED_OFF_L = 0xFC;
        public const byte ALLED_OFF_H = 0xFD;

        private bool DebugOutput;
        private II2CDevice I2CDevice;
        private byte I2CAddress;

        public object i2cDeviceLock;
        

        public PCA9685(byte address = 0x40, bool debug = false)
        {
            I2CDevice = Pi.I2C.AddDevice(address /*0x20*/);
            I2CAddress = address;
            DebugOutput = debug;

            if (DebugOutput)
            {
                Console.WriteLine("Reseting PCA9685");
            }

            WriteData(MODE1, 0x00);
        }

        private void WriteData(byte register, byte value) {

            lock (i2cDeviceLock)
            {
                //Writes an 8-bit value to the specified register/address
                I2CDevice.WriteAddressByte(register, value);
            }
            if (DebugOutput)
            {
                Console.WriteLine("I2C: Write {0:02X} to register {1:02X}", (value, register));
            }
        }


        byte ReadData(byte register) {
            //Read an unsigned byte from the I2C device
            byte result;
            lock (i2cDeviceLock)
            {
                 result = I2CDevice.ReadAddressByte(register);
            }
            
            if (DebugOutput) 
            {
                Console.WriteLine("I2C: Device {0:02X} returned {1:02X} from reg {2:02X}", (this.I2CAddress, result & 0xFF, register));
            }
            return result;
        }

        public void setPWMFreq(int freq){
            //Sets the PWM frequency
            float prescaleval = 25000000.0f; // 25MHz
            prescaleval /= 4096.0f; // 12-bit
            prescaleval /= freq;
            prescaleval -= 1.0f;

            if (DebugOutput)
            { 
                Console.WriteLine("Setting PWM frequency to {0} Hz" , freq);
                Console.WriteLine("Estimated pre-scale: {0}",prescaleval);
            }
            float prescale = (float) Math.Floor(prescaleval + 0.5f); //kinda yucky. not sure about this.

            if (DebugOutput)
                Console.WriteLine("Final pre-scale: {0}", prescale);

            var oldmode = ReadData(MODE1);
            byte newmode = (byte) ((oldmode & 0x7F) | 0x10);              // prep sleep command
            WriteData(MODE1, newmode);                      // send 'go to sleep'
            WriteData(PRESCALE, (byte)Math.Floor(prescale));
            WriteData(MODE1, oldmode);
            Thread.Sleep(5); //0.005s should be 5 ms right?
            WriteData(MODE1, (byte) (oldmode | 0x80));
        }

        //assuming on/ off are bytes or this would not make much sense
        protected void SetPWM(byte channel, byte on, byte off) {

            byte offset = (byte)(0x04 * channel);

            //Sets a single PWM channel
            WriteData((byte) (LED0_ON_L  + offset), (byte) (on & 0xFF      ));
            WriteData((byte) (LED0_ON_H  + offset), (byte) (on >> 8        ));
            WriteData((byte) (LED0_OFF_L + offset), (byte) (off & 0xFF     ));
            WriteData((byte) (LED0_OFF_H + offset), (byte) (off >> 8       ));

            if (this.DebugOutput)
                Console.WriteLine("channel: {0}  LED_ON: {1} LED_OFF: {2}", channel, on, off);
        }

        public void setServoPulse(byte channel, int pulse) {
            lock (i2cDeviceLock)
            {
                //Sets the Servo Pulse,The PWM frequency must be 50HZ
                //PWM frequency is 50HZ,the period is 20000us
                pulse = pulse * 4096 / 20000;
                SetPWM(channel, 0, (byte)pulse);       //what? are we sure pulse is byte and not int?
            }
        }

    }
}
