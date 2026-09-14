// VehicleController.cs — carro arcade para móvil
// Física simple y divertida (no realista): botón para acelerar,
// giro con joystick o botones. El carro se queda pegado al suelo.
using UnityEngine;

namespace Geayi.Vehicles
{
    public class VehicleController : MonoBehaviour
    {
        [Header("Arcade")]
        public float acceleration = 14f;
        public float maxSpeed = 20f;
        public float turnRate = 130f; // grados por segundo
        public float drag = 6f;       // frenado natural al soltar el acelerador

        [Header("Entrada (la pone la UI)")]
        [Tooltip("true mientras el botón 🟢 ACELERAR está presionado")]
        public bool throttle = false;
        [Tooltip("-1 izquierda, +1 derecha (joystick o botones)")]
        public float steer = 0f;

        [Header("Conductor")]
        public Transform driverSeat;
        public bool occupied = false;

        private float speed = 0f;
        private Transform driver;

        void Update()
        {
            if (!occupied)
            {
                // Sin conductor: frenar hasta parar
                speed = Mathf.MoveTowards(speed, 0f, drag * Time.deltaTime);
            }
            else
            {
                if (throttle)
                    speed = Mathf.MoveTowards(speed, maxSpeed, acceleration * Time.deltaTime);
                else
                    speed = Mathf.MoveTowards(speed, 0f, drag * Time.deltaTime);

                // Girar (más fácil cuando va rápido, imposible quieto)
                float grip = Mathf.Clamp01(Mathf.Abs(speed) / 4f);
                transform.Rotate(0f, steer * turnRate * grip * Time.deltaTime, 0f);
            }

            // Avanzar
            transform.Translate(0f, 0f, speed * Time.deltaTime, Space.Self);

            // Ciudad plana: pegado al suelo
            Vector3 p = transform.position;
            p.y = 0f;
            transform.position = p;

            // El conductor viaja en el asiento
            if (driver != null && driverSeat != null)
                driver.position = driverSeat.position;
        }

        // ---- Estos métodos los llama la UI táctil ----
        public void SetThrottle(bool on) { throttle = on; }
        public void SetSteer(float value) { steer = Mathf.Clamp(value, -1f, 1f); }

        // Subir al carro (oculta al jugador mientras maneja)
        public void Enter(Transform playerTransform)
        {
            driver = playerTransform;
            occupied = true;
            speed = 0f;
            playerTransform.gameObject.SetActive(false);
        }

        // Bajar del carro (aparece al lado)
        public void Exit()
        {
            if (driver != null)
            {
                driver.gameObject.SetActive(true);
                driver.position = transform.position + transform.right * 2.5f;
                driver.rotation = transform.rotation;
            }
            driver = null;
            occupied = false;
            throttle = false;
            steer = 0f;
            speed = 0f;
        }

        // Construye un carro simple con primitivas (para pruebas rápidas)
        public static VehicleController CreateSimpleCar(Vector3 position)
        {
            GameObject car = new GameObject("Car");
            car.transform.position = position;

            Material bodyMat = new Material(Shader.Find("Standard"));
            bodyMat.color = new Color(0.90f, 0.15f, 0.20f);
            Material darkMat = new Material(Shader.Find("Standard"));
            darkMat.color = new Color(0.10f, 0.10f, 0.12f);

            AddBox(car.transform, "Body", new Vector3(0f, 0.7f, 0f), new Vector3(2f, 0.7f, 4f), bodyMat);
            AddBox(car.transform, "Cabin", new Vector3(0f, 1.35f, -0.3f), new Vector3(1.7f, 0.6f, 2f), darkMat);
            AddWheel(car.transform, "WheelFL", new Vector3(-1.05f, 0.4f, 1.3f), darkMat);
            AddWheel(car.transform, "WheelFR", new Vector3(1.05f, 0.4f, 1.3f), darkMat);
            AddWheel(car.transform, "WheelBL", new Vector3(-1.05f, 0.4f, -1.3f), darkMat);
            AddWheel(car.transform, "WheelBR", new Vector3(1.05f, 0.4f, -1.3f), darkMat);

            GameObject seat = new GameObject("DriverSeat");
            seat.transform.SetParent(car.transform);
            seat.transform.localPosition = new Vector3(0f, 1f, -0.3f);

            VehicleController vc = car.AddComponent<VehicleController>();
            vc.driverSeat = seat.transform;
            return vc;
        }

        private static void AddBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().material = mat;
        }

        private static void AddWheel(Transform parent, string name, Vector3 pos, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(0.8f, 0.3f, 0.8f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, 90f); // eje horizontal
            go.GetComponent<Renderer>().material = mat;
        }
    }
}
