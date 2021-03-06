﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YoumuSlashPlayerController : MonoBehaviour
{
    [SerializeField]
    private YoumuSlashTimingData timingData;
    [SerializeField]
    private Animator rigAnimator;
    [SerializeField]
    private Transform facingTransform;
    [SerializeField]
    private Transform facingSpriteTransform;
    [SerializeField]
    private YoumuSlashSpriteTrail spriteTrail;
    [SerializeField]
    private float spriteTrailStartOffset;
    [SerializeField]
    private bool firstTargetStareMode;

    [Header("Timing window in seconds for hitting an object")]
    [SerializeField]
    private Vector2 hitTimeFudge;

    [Header("Debug values")]
    [SerializeField]
    private bool autoSlash;
    [SerializeField]
    private Vector2 sliceAngleRange;
    [SerializeField]
    private AudioClip debugSound;
    [SerializeField]
    private AudioClip hitVoiceClip;
    [SerializeField]
    private AudioClip screamClip;
    
    int nextIdleBeat = -1;
    int untenseBeat = -1;
    bool attacking;
    int beatResetTimer;
    bool attackedUp;
    private YoumuSlashBeatMap.TargetBeat.Direction lastSliceDirection;

    private void Start()
    {
        YoumuSlashTimingController.onBeat += onBeat;
        YoumuSlashTargetSpawner.OnTargetLaunch += onTargetLaunched;
    }

    void onTargetLaunched(YoumuSlashBeatMap.TargetBeat target)
    {
        if ((!firstTargetStareMode || timingData.BeatMap.getFirstActiveTarget(timingData.CurrentBeat, hitTimeFudge.y) == target)
            && !attacking
            && getFirstHittableTarget(YoumuSlashBeatMap.TargetBeat.Direction.Any) == null)
            rigAnimator.SetBool("LookBack", isFacingRight() != (target.HitDirection == YoumuSlashBeatMap.TargetBeat.Direction.Right));
        if (autoSlash)
            Invoke("performAutoSlash", (target.HitBeat - timingData.CurrentBeat) * timingData.BeatDuration);
    }

    void performAutoSlash()
    {
        attemptSlash(YoumuSlashBeatMap.TargetBeat.Direction.Any);
    }

    void onBeat(int beat)
    {
        if (beat >= nextIdleBeat)
        {
            handleIdleAnimation(beat);
        }
        rigAnimator.SetBool("IsAttacking", attacking);
        
        rigAnimator.SetTrigger("Beat");
        beatResetTimer = 2;
    }

    void handleIdleAnimation(int beat)
    {
        var nextTarget = timingData.BeatMap.getFirstActiveTarget((float)beat, hitTimeFudge.y);
        //var lastTarget = timingData.BeatMap.getLastActiveTarget((float)beat, hitTimeFudge.y);

        if (beat >= untenseBeat)
            rigAnimator.SetBool("Tense", false);

        if (beat == nextIdleBeat)
        {
            if (nextTarget != null && beat >= (int)nextTarget.HitBeat)
            {
                //Delay idle if new target in 1 beat
                nextIdleBeat++;
                return;
            }
            else
            {
                //Return to idle
                MicrogameController.instance.playSFX(debugSound);
                if (attacking)
                {
                    rigAnimator.ResetTrigger("Attack");
                    rigAnimator.SetTrigger("Idle");
                }
                attacking = false;
            }
        }
        if (beat >= nextIdleBeat)
        {
            //Start bobbing on return
            rigAnimator.SetTrigger("Bob");
        }

        if (nextTarget != null)
        {
            if (beat + 1 >= (int)nextTarget.HitBeat)
            {
                //1 Beat prep
                bool initialFlip = isFacingRight();
                bool flipIdle = nextTarget.HitDirection == YoumuSlashBeatMap.TargetBeat.Direction.Right;
                if (isRigFacingRight())
                    flipIdle = !flipIdle;
                setIdleFlipped(flipIdle);

                if (isFacingRight() != initialFlip)
                {
                    rigAnimator.SetBool("LookBack", false);
                    rigAnimator.SetTrigger("ResetLook");
                }
                rigAnimator.SetBool("Tense", true);
                untenseBeat = beat + 2;

                nextIdleBeat = beat + 2;
            }
            else if (beat + 2 >= (int)nextTarget.HitBeat)
            {
                //2 Beat prep
            }
        }
        attacking = false;
        rigAnimator.SetBool("Prep", false);


    }

    void setRigFacingRight(bool facingRight)
    {
        facingTransform.localScale = new Vector3(Mathf.Abs(facingTransform.localScale.x) * (facingRight ? -1f : 1f),
            facingTransform.localScale.y, facingTransform.localScale.z);
        rigAnimator.SetBool("FacingRight", facingRight);
    }

    void setIdleFlipped(bool flip)
    {
        facingSpriteTransform.localScale = new Vector3(Mathf.Abs(facingSpriteTransform.localScale.x) * (flip ? -1f : 1f),
            facingSpriteTransform.localScale.y, facingSpriteTransform.localScale.z);
    }

    bool isRigFacingRight()
    {
        return facingTransform.localScale.x < 0f;
    }

    bool isFacingRight()
    {
        bool spriteFlipped = facingSpriteTransform.localScale.x < 0f;
        return isRigFacingRight() ? !spriteFlipped : spriteFlipped;
    }

    public void setBobEnabled(bool enable)
    {
        rigAnimator.SetBool("EnableBob", enable);
    }

    public void setTenseEnabled(bool enable)
    {
        rigAnimator.SetBool("EnableTense", enable);
    }

    public void setForceTense(bool forceTense)
    {
        rigAnimator.SetBool("ForceTense", forceTense);
    }

    public void setEyesClosed(bool closed)
    {
        rigAnimator.SetBool("EyesClosed", closed);
    }

    void Update ()
    {
        if (beatResetTimer > 0)
        {
            beatResetTimer--;
            if (beatResetTimer <= 0)
            {
                rigAnimator.ResetTrigger("Beat");
            }
        }
        handleInput();
	}

    void handleInput()
    {
        var directionPressed = YoumuSlashBeatMap.TargetBeat.Direction.Any;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            directionPressed = YoumuSlashBeatMap.TargetBeat.Direction.Left;
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            directionPressed = YoumuSlashBeatMap.TargetBeat.Direction.Right;

        if (directionPressed != YoumuSlashBeatMap.TargetBeat.Direction.Any)
        {
            attemptSlash(directionPressed);
        }

    }

    YoumuSlashBeatMap.TargetBeat getFirstHittableTarget(YoumuSlashBeatMap.TargetBeat.Direction direction)
    {
        return timingData.BeatMap.getFirstHittableTarget(timingData.CurrentBeat,
            hitTimeFudge.x / timingData.BeatDuration, hitTimeFudge.y / timingData.BeatDuration, direction);
    }

    void attemptSlash(YoumuSlashBeatMap.TargetBeat.Direction direction)
    {
        var hitTarget = getFirstHittableTarget(direction);
        if (hitTarget != null)
        {
            rigAnimator.SetBool("IsAttacking", true);
            bool reAttacking = attacking == true && hitTarget.HitDirection == lastSliceDirection;
            rigAnimator.SetBool("ReAttack", reAttacking);
            lastSliceDirection = hitTarget.HitDirection;
            bool attackingUp = (!attackedUp && attacking) || hitTarget.ForceUp;
            rigAnimator.SetBool("AttackUp", attackingUp);
            attackedUp = attackingUp;

            attacking = true;

            direction = hitTarget.HitDirection;
            hitTarget.launchInstance.slash(MathHelper.randomRangeFromVector(sliceAngleRange));
            nextIdleBeat = (int)hitTarget.HitBeat + 1;
            bool facingRight = direction == YoumuSlashBeatMap.TargetBeat.Direction.Right;
            setRigFacingRight(facingRight);
            setIdleFlipped(false);

            rigAnimator.SetTrigger("Attack");
            rigAnimator.ResetTrigger("Idle");
            rigAnimator.SetBool("Tense", false);
            untenseBeat = -1;
            rigAnimator.SetBool("LookBack", false);
            rigAnimator.SetTrigger("ResetLook");

            switch (hitTarget.HitEffect)
            {
                case (YoumuSlashBeatMap.TargetBeat.Effect.Scream):
                    rigAnimator.SetTrigger("Scream");
                    nextIdleBeat++;
                    MicrogameController.instance.playSFX(screamClip);
                    break;
                default:
                    MicrogameController.instance.playSFX(hitVoiceClip, pitchMult: Random.Range(.95f, 1.05f));
                    break;
            }
            
            float facingDirection = (isRigFacingRight() ? -1f : 1f);
            spriteTrail.resetTrail(spriteTrailStartOffset * facingDirection);
        }
    }
}
