using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;


public class PlayerController : MonoBehaviour
{
    private enum GameState { Ready, Playing, Ended }

    public float forwardSpeed = 2.5f;
    public float jumpForce = 6f;
    public float fallLimitY = -5.6f;

    public AudioClip flyClip;
    public AudioClip coinClip;
    public AudioClip explodeClip;
    public AudioClip winClip;
    public float flyVolume = 0.5f;

    TMP_Text hintText;
    TMP_Text scoreText;
    public TMP_Text resultText;

    GameObject explosionPrefab;
    public float restartDelay = 1f;

    private Rigidbody rb;
    private AudioSource loopSource;
    private AudioSource sfxSource;
    private InputAction tapAction;
    private GameState state = GameState.Ready;
    private int score;
    private float endTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.clip = flyClip;
        loopSource.playOnAwake = false;
        loopSource.loop = false;
        loopSource.volume = flyVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        
        tapAction = new InputAction("Tap", InputActionType.Button);
        tapAction.AddBinding("<Pointer>/press");
        tapAction.AddBinding("<Keyboard>/space");
    }

    void OnEnable() => tapAction.Enable();
    void OnDisable() => tapAction.Disable();
    void OnDestroy() => tapAction.Dispose();

    void Start()
    {
        SetText(resultText, "");
    }

    void Update()
    {
        bool tap = tapAction.WasPressedThisFrame();

        switch (state)
        {
            case GameState.Ready:
                if (tap) StartGame();
                break;

            case GameState.Playing:
                if (tap) Jump();
                if (transform.position.y < fallLimitY) Lose();
                break;

            case GameState.Ended:
                if (tap && Time.time - endTime > restartDelay)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;
        }
    }

    void FixedUpdate()
    {
        if (state != GameState.Playing) return;

        Vector3 v = rb.linearVelocity;
        v.x = forwardSpeed;
        rb.linearVelocity = v;
    }

    void StartGame()
    {
        state = GameState.Playing;
        rb.useGravity = true;
        loopSource.Stop();
        Jump();
    }

    void Jump()
    {
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        if (flyClip != null)
        {
            loopSource.Stop();
            loopSource.Play();
        }
    }

    void Lose()
    {
        EndGame("You Lose", explodeClip);
    }

    void Win()
    {
        EndGame("You Win!", winClip);
    }

    void EndGame(string message, AudioClip clip)
    {
        state = GameState.Ended;
        endTime = Time.time;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        loopSource.Stop();
        PlaySfx(clip);
        SetText(resultText, message + "\n<size=50%>Tap to Restart</size>");
    }

    void OnTriggerEnter(Collider other) => HandleHit(other);
    void OnCollisionEnter(Collision collision) => HandleHit(collision.collider);

    void HandleHit(Collider other)
    {
        if (state != GameState.Playing) return;

        if (other.CompareTag("Coin"))
        {
            other.enabled = false;
            Destroy(other.gameObject);
            PlaySfx(coinClip);
        }
        else if (other.CompareTag("Obstacle"))
        {
            Lose();
        }
        else if (other.CompareTag("Finish"))
        {
            Win();
        }
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    static void SetText(TMP_Text t, string s)
    {
        if (t != null) t.text = s;
    }
}