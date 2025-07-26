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
    private bool isCrouching;

    Vector2 input;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        input.Normalize();

        isRunning = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
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
            else if (input.magnitude > 0.1f)
            {
                float currentSpeed = isRunning ? kosmaHiz : (isCrouching ? hareketHiz * 0.5f : hareketHiz);
                Vector3 hareket = hareketHesapla(currentSpeed);
                rb.AddForce(hareket, ForceMode.VelocityChange);
            }
            else
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.2f, rb.linearVelocity.y, rb.linearVelocity.z * 0.2f);
            }
        }
        else
        {
            if (input.magnitude > 0.1f)
            {
                float airSpeed = isRunning ? kosmaHiz * airControl : hareketHiz * airControl;
                Vector3 hareket = hareketHesapla(airSpeed);
                rb.AddForce(hareket, ForceMode.VelocityChange);
            }
        }

        isGrounded = false;
        isJumping = false;
    }

    private void LateUpdate()
    {
        float hedefHiz = isRunning ? 1.0f : 0.2f;
        if (Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
        {
            float hiz = isCrouching ? 0.1f : (isRunning ? 1f : 0.2f);
            anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), hiz, Time.deltaTime * 10f));
        }
        else
        {
            anim.SetFloat("speed", 0);
        }

        animasyon.Sol_Hareket(anim, "solHareket", animasyon.ParamtereOlustur(Sol_Yon_Parametreleri));
        animasyon.Sag_Hareket(anim, "sagHareket", animasyon.ParamtereOlustur(Sag_Yon_Parametreleri));
        animasyon.Geri_Hareket(anim, "geri");
        animasyon.Egilme_Hareket(anim, "egilmeHareket", animasyon.ParamtereOlustur(Egilme_Yon_Parametreleri));
    }

    private void OnTriggerStay(Collider other)
    {
        isGrounded = true;
    }

    Vector3 hareketHesapla(float hiz)
    {
        Vector3 hedefHiz = new Vector3(input.x, 0, input.y);
        hedefHiz = transform.TransformDirection(hedefHiz);
        hedefHiz *= hiz;

        Vector3 mevcutHiz = rb.linearVelocity;
        Vector3 hizFarki = hedefHiz - mevcutHiz;
        hizFarki.x = Mathf.Clamp(hizFarki.x, -MaxHizDegisimi, MaxHizDegisimi);
        hizFarki.z = Mathf.Clamp(hizFarki.z, -MaxHizDegisimi, MaxHizDegisimi);
        hizFarki.y = 0f;

        return hizFarki;
    }
}
