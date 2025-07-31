using Fusion;
using UnityEngine;
using Kutuphanem;

public class Hareket : NetworkBehaviour
{
    private Rigidbody rb;
    private Animator anim;

    [Header("Hareket Ayarlarý")]
    public float hareketHiz = 4f;
    public float MaxHizDegisimi = 10f;
    public float kosmaHiz = 6f;
    public float airControl = 0.5f;
    public float ziplamaSiniri = 4f;

    [Header("Animator Ayarlarý")]
    MyLibrary animasyon = new MyLibrary();
    float[] Sol_Yon_Parametreleri = { 0.15f, 0.5f, 1 };
    float[] Sag_Yon_Parametreleri = { 0.15f, 0.5f, 1 };
    float[] Egilme_Yon_Parametreleri = { 0.15f, 0.25f, 0.50f, 0.75f, 1f };

    private bool isGrounded;

    [Networked] private Vector2 input { get; set; }
    [Networked] private bool isRunning { get; set; }
    [Networked] private bool isJumping { get; set; }
    [Networked] private bool isCrouching { get; set; }


    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        
    }

    public override void FixedUpdateNetwork()
    {
        // Eðer bu client input otoritesine sahipse hareket ve input iþleme
        if (Object.HasStateAuthority)
        {
            if (GetInput<NetworkInputData>(out var data))
            {
                input = data.move.normalized;
                isRunning = data.run;
                isCrouching = data.crouch;
                isJumping = data.jump;

                // Animasyon güncelle
                animasyon.Sol_Hareket(anim, "solHareket", animasyon.ParamtereOlustur(Sol_Yon_Parametreleri), data);
                animasyon.Sag_Hareket(anim, "sagHareket", animasyon.ParamtereOlustur(Sag_Yon_Parametreleri), data);
                animasyon.Geri_Hareket(anim, "geri", data);
                animasyon.Egilme_Hareket(anim, "egilmeHareket", animasyon.ParamtereOlustur(Egilme_Yon_Parametreleri), data);

                if (data.move.y > 0.1f)
                {
                    float hiz = isCrouching ? 0.1f : (isRunning ? 1f : 0.2f);
                    anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), hiz, Time.deltaTime * 10f));
                }
                else
                {
                    anim.SetFloat("speed", 0);
                }

                float speed = isRunning ? kosmaHiz : (isCrouching ? hareketHiz * 0.5f : hareketHiz);

                if (isGrounded)
                {
                    if (isJumping)
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, ziplamaSiniri, rb.linearVelocity.z);
                    }
                    else if (input.magnitude > 0.1f)
                    {
                        Vector3 hareket = hareketHesapla(speed);
                        rb.AddForce(hareket, ForceMode.VelocityChange);
                    }
                    else
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.2f, rb.linearVelocity.y, rb.linearVelocity.z * 0.2f);
                    }
                }
                else
                {
                    float airSpeed = speed * airControl;
                    Vector3 hareket = hareketHesapla(airSpeed);
                    rb.AddForce(hareket, ForceMode.VelocityChange);
                }

                isJumping = false;
                isGrounded = false;

            }

        }
    }
    private Vector3 hareketHesapla(float hiz)
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

    private void OnTriggerStay(Collider other)
    {
        isGrounded = true;
    }
}
