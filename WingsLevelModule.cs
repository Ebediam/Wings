using UnityEngine;
using ThunderRoad;
using OVR;
using Valve.VR;

namespace Wings
{
    public class WingsLevelModule : LevelModule
    {
        bool hasReleased = false;
        bool isFlying = false;
        Rigidbody rb;

        public delegate void WingsEvent(bool active);
        public static WingsEvent DeactivateWings;

        bool isActive = true;
        public static WingsLevelModule local;

        float oldMass;
        float oldSpeed;

        bool thumbLastState;
        bool thumbState;

        public float airSpeedMultiplier = 3f;
        float flySpeed;
        bool speedStored;
        float oldDrag = 1000f;
        float oldMaxAngle;
        public float flyAcceleration = 10f;

        public override void OnLevelLoaded(LevelDefinition levelDefinition)
        {
            initialized = true; // Set it to true when your script are loaded
            Debug.Log("--------- WINGS LOADED -----------");

            local = this;

            if (PlayerControl.driver == PlayerControl.Driver.Oculus)
            {
                Debug.Log("OVRPlugin has input focus (Oculus)");
            }
            else
            {
                Debug.Log("SteamVR has input focus (Steam)");
                SteamVR_Actions.default_Jump.onChange += OnJumpPress;
            }

            DeactivateWings += SetWingsActive;
            
        }
        
        public void JumpButtonPress(bool pushed)
        {
            if (!isActive)
            {
                return;
            }

            if (!Player.local)
            {
                return;
            }
            else
            {
                if (!Player.local.body)
                {
                    return;
                }
                else
                {
                    DestabilizeHeldNPC();

                    if (!Player.local.locomotion.isGrounded)
                    {

                        if (isFlying)
                        {
                            if (pushed)
                            {
                                if (hasReleased)
                                {
                                    DeactivateFly();
                                }

                            }
                            else
                            {
                                hasReleased = true;
                            }

                            return;
                        }
                        else
                        {
                            if (!pushed)
                            {
                                hasReleased = true;
                            }
                            else
                            {
                                if (hasReleased)
                                {
                                    ActivateFly();
                                }
                            }
                        }




                    }


                }


            }
        }

        public void OnJumpPress(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
        {
            JumpButtonPress(newState);
        }

        public override void Update(LevelDefinition levelDefinition)
        {
            if (!isActive)
            {
                return;
            }

            if (!Player.local)
            {
                return;
            }
            else
            {
                if (!Player.local.body)
                {
                    return;
                }
                else
                {
                    thumbState = OVRInput.GetDown(OVRInput.RawButton.RThumbstick, OVRInput.Controller.Active);


                    if (!Player.local.locomotion.isGrounded)
                    {
                        if (OVRPlugin.hasInputFocus)
                        {
                            if (thumbState)
                            {
                                if (!thumbLastState)
                                {
                                    if (isFlying)
                                    {
                                        DeactivateFly();
                                    }
                                    else
                                    {
                                        ActivateFly();

                                    }

                                    
                                }
                            }
                        }
                        






                        if (isFlying)
                        {
                            DestabilizeHeldNPC();

                            if (OVRPlugin.hasInputFocus)
                            {
                                Vector2 turnAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

                                if(turnAxis.y!= 0)
                                {
                                    MoveFlyingPlayerUp(turnAxis.y);
                                }
                            }
                            else
                            {
                                Vector2 turnAxis = SteamVR_Actions.default_Turn.axis;

                                if (turnAxis.y != 0)
                                {
                                    MoveFlyingPlayerUp(turnAxis.y);
                                }
                            }
                            

                      
                        }


                    }
                    else
                    {
                        if (isFlying)
                        {
                            DeactivateFly();
                        }
                    }

                    thumbLastState = thumbState;
                }

            }
        }


        public void MoveFlyingPlayerUp(float amount)
        {
            if (!isActive)
            {
                return;
            }

            if (Pointer.GetActive())
            {
                if (Pointer.GetActive().isPointingUI)
                {
                    return;
                }
            }
 
            rb.AddForce(Vector3.up * flyAcceleration * amount, ForceMode.Acceleration);
            

            
        }


        public void DestabilizeHeldNPC()
        {
            if (Player.local.handLeft.bodyHand.interactor.grabbedHandle)
            {
                Creature npc = Player.local.handLeft.bodyHand.interactor.grabbedHandle.gameObject.GetComponentInParent<Creature>();
                if (npc)
                {
                    if (npc.ragdoll.state == Creature.State.Dead)
                    {
                        return;
                    }
                    else
                    {
                        npc.ragdoll.SetState(Creature.State.Destabilized);
                    }


                }
                else
                {

                    foreach(Interactor handler in Player.local.handLeft.bodyHand.interactor.grabbedHandle.handlers)
                    {
                        Creature creatureHandler = handler.gameObject.GetComponentInParent<Creature>();

                        if (creatureHandler)
                        {
                            if (creatureHandler != Creature.player)
                            {
                                handler.TryRelease();
                            }
                        }


                    }

                }


            }
            else if (Player.local.handRight.bodyHand.interactor.grabbedHandle)
            {
                Creature npc = Player.local.handRight.bodyHand.interactor.grabbedHandle.gameObject.GetComponentInParent<Creature>();
                if (npc)
                {
                    if (npc.ragdoll.state == Creature.State.Dead)
                    {
                        return;
                    }
                    else
                    {
                        npc.ragdoll.SetState(Creature.State.Destabilized);
                    }


                }
                else
                {
                    foreach (Interactor handler in Player.local.handRight.bodyHand.interactor.grabbedHandle.handlers)
                    {
                        Creature creatureHandler = handler.gameObject.GetComponentInParent<Creature>();

                        if (creatureHandler)
                        {
                            if (creatureHandler != Creature.player)
                            {
                                handler.TryRelease();
                            }
                        }


                    }

                }
            }
        }

        public void SetWingsActive(bool active)
        {
            isActive = active;
        }

        public void ActivateFly()
        {
            

            if (!speedStored)
            {
                oldSpeed = Player.local.locomotion.airSpeed;
                flySpeed = oldSpeed * airSpeedMultiplier;
                speedStored = true;
                oldMaxAngle = Player.local.locomotion.groundMaxAngle;
            }

            Player.local.locomotion.groundMaxAngle = -359f;
            rb = Player.local.locomotion.rb;
            if (oldDrag == 1000f)
            {
                oldDrag = rb.drag;
                oldMass = rb.mass;
            }

            rb.mass = 100000f;
            rb.drag = 0.9f;
            rb.useGravity = false;
            Player.local.locomotion.velocity = Vector3.zero;
            Player.local.locomotion.airSpeed = flySpeed;
            isFlying = true;

        }

        public void DeactivateFly()
        {
            Player.local.locomotion.groundMaxAngle = oldMaxAngle;
            isFlying = false;
            rb.drag = oldDrag;
            hasReleased = false;
            rb.useGravity = true;
            rb.mass = oldMass;
            Player.local.locomotion.airSpeed = oldSpeed;
            
        }

        public void DebugInput()
        {
            if (PlayerControl.handRight.alternateUseAxis > 0)
            {
                Debug.Log("AlternateUseAxis > 0");
            }

            if (PlayerControl.handRight.alternateUsePressed)
            {
                Debug.Log("AlternateUsePressed");
            }

            if (PlayerControl.handRight.pinchPressed)
            {
                Debug.Log("Pinch pressed");
            }

            if(PlayerControl.handRight.thumbCurl > 0.1)
            {
                Debug.Log("thumbCurl: " + PlayerControl.handRight.thumbCurl);
            }

            if(PlayerControl.handRight.useAxis > 0.1)
            {
                Debug.Log("Use axis: " + PlayerControl.handRight.useAxis);
            }

            if (PlayerControl.handRight.castPressed)
            {
                Debug.Log("Cast pressed");
            }

            if (PlayerControl.handRight.indexCurl > 0.1)
            {
                Debug.Log("IndexCurl: "+PlayerControl.handRight.indexCurl);
            }

            if (PlayerControl.handRight.usePressed)
            {
                Debug.Log("Use pressed");
            }

            if(PlayerControl.handRight.gripAxis > 0.1)
            {
                Debug.Log("Grip axis: " + PlayerControl.handRight.gripAxis);
   
            }
            

           

        }



    }
}

