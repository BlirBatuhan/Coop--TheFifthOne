using UnityEngine;


public class Hareket : MonoBehaviour
{
    private Rigidbody rb;
    public float hareketHiz = 4f;
    public float MaxHizDegisimi = 10f;
    public float kosmaHiz = 6f;


    public float airControl = 0.5f;
    public float ziplamaSiniri = 4f;


    private bool isRunning;
    private bool isJumping;
    private bool isGrounded;

    Vector2 ýnput;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
    }

   
    void Update()
    {
        ýnput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        ýnput.Normalize();

        isRunning = Input.GetKey(KeyCode.LeftShift);
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            isJumping = true;
        }

    }

    private void FixedUpdate()
    {
        if (isGrounded)
        {
            if (isJumping)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, ziplamaSiniri, rb.linearVelocity.z);
            }
            else if (ýnput.magnitude > 0.5f)
            {
               
                rb.AddForce(hareketHesapla(isRunning ? kosmaHiz : hareketHiz), ForceMode.VelocityChange);

            }
            else
            {
               

                var velocity1 = rb.linearVelocity;
                velocity1 = new Vector3(velocity1.x * 0.2f * Time.fixedDeltaTime, velocity1.y, velocity1.z * 0.2f * Time.fixedDeltaTime); // Y eksenindeki h?z? s?f?rla
                rb.linearVelocity = velocity1;

            }

        }
        else
        {
            if (ýnput.magnitude > 0.5f)
            {
                rb.AddForce(hareketHesapla(isRunning ? kosmaHiz * airControl : hareketHiz * airControl), ForceMode.VelocityChange);

            }
            else
            {
                var velocity1 = rb.linearVelocity;
                velocity1 = new Vector3(velocity1.x * 0.2f * Time.fixedDeltaTime, velocity1.y, velocity1.z * 0.2f * Time.fixedDeltaTime); // Y eksenindeki h?z? s?f?rla
                rb.linearVelocity = velocity1;
            }
        }

        isGrounded = false;
        isJumping = false;
    }





    private void OnTriggerStay(Collider other)
    {
        /*if (pv == null) return; // Bu satýrý ekle
        if (!pv.IsMine) return;*/
        isGrounded = true;
    }

    Vector3 hareketHesapla(float _hiz)
    {
        Vector3 hedefHiz = new Vector3(ýnput.x, 0, ýnput.y);
        hedefHiz = transform.TransformDirection(hedefHiz);

        hedefHiz *= _hiz;

        Vector3 hiz = rb.linearVelocity;

        if (ýnput.magnitude > 0.5f)
        {
            Vector3 hizFarki = hedefHiz - hiz;
            hizFarki.x = Mathf.Clamp(hizFarki.x, -MaxHizDegisimi, MaxHizDegisimi);
            hizFarki.z = Mathf.Clamp(hizFarki.z, -MaxHizDegisimi, MaxHizDegisimi);

            hizFarki.y = 0f; // Y eksenindeki h?z? s?f?rla

            return (hizFarki);
        }
        else
        {
            return new Vector3();
        }
    }

}
