using UnityEngine;
using Kutuphanem;

public class Hareket : MonoBehaviour
{
    private Rigidbody rb;
    public float hareketHiz = 4f;
    public float MaxHizDegisimi = 10f;
    public float kosmaHiz = 6f;


    MyLibrary animasyon = new MyLibrary();


    public float airControl = 0.5f;
    public float ziplamaSiniri = 4f;

    [Header("Animator Settings")]
    private Animator anim;
    float[] Sol_Yon_Parametreleri = { 0.15f, 0.5f, 1 };
    float[] Sag_Yon_Parametreleri = { 0.15f, 0.5f, 1 };
    float[] Egilme_Yon_Parametreleri = { 0.15f, 0.25f, 0.50f, 0.75f, 1f };

    private bool isRunning;
    private bool isJumping;
    private bool isGrounded;

    Vector2 ýnput;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        
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

    private void LateUpdate()
    {
        if (Input.GetKey(KeyCode.W))
        {
            if (isRunning) // Koþma
            {
                float newSpeed = isRunning ? 1.0f : 0.2f;
                anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), newSpeed, Time.deltaTime * 10f));
            }
            else // Yürüme
                anim.SetFloat("speed", 0.1f);
        }
        else
        {
            anim.SetFloat("speed", 0); // Durma
        }

        animasyon.Sol_Hareket(anim, "solHareket", animasyon.ParamtereOlustur(Sol_Yon_Parametreleri));
        animasyon.Sag_Hareket(anim, "sagHareket", animasyon.ParamtereOlustur(Sag_Yon_Parametreleri));
        animasyon.Geri_Hareket(anim, "geri");

        // **Eðilme animasyonu çaðrýlýrken hýz azaltýlýyor**
        animasyon.Egilme_Hareket(anim, "egilmeHareket", animasyon.ParamtereOlustur(Egilme_Yon_Parametreleri), ref hareketHiz);




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
                anim.SetFloat("speed", 0.0f);

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
