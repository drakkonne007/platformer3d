using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace GiantAI
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class GiantAI : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private LayerMask playerLayer;

        [Header("Combat Settings")]
        [SerializeField] private bool canThrow = true;
        [SerializeField] private float throwDistance = 10f;
        [SerializeField] private float throwCooldown = 5f;

        [SerializeField] private float attackDistance = 3f;
        [SerializeField] private float attackCooldown = 3f;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("References")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private Collider dialogColllider_;
        [Space(5)]
        [SerializeField] public GameObject stanIcon;
        [SerializeField] public GameObject attackIcon;
        [SerializeField] GameObject _dialogAttention;
        [SerializeField] GameObject _dangerSaw;
        [SerializeField] GameObject crystall_;
        [SerializeField] GameObject _shadowNpc;
        [SerializeField] GameObject bloodParticles_;

        [Header("Randomization")]
        [SerializeField][Range(0, 100)] private int idleProbability = 30;

        [Header("RpgSettings")]
        [SerializeField] bool citizen = false;
        [SerializeField] bool isTreater = false;
        [SerializeField] bool noDialog = false;
        [SerializeField] bool hasSecondAttack = false;
        [SerializeField] public bool isRangeFirst = false;                
        [SerializeField] int countSuperDash = 0;
        [SerializeField] float health = 100;
        [SerializeField] float armor = 10;
        [SerializeField] public DQuestTriggerParent questHandler;
        [SerializeField] public HPBarChanger _hitBar;
        [SerializeField] List<GameObject> loots;

        [Header("Colliders")]
        [SerializeField] public List<Collider> hitBoxColl_;
        [SerializeField] public List<Collider> weaponColl_;
        [SerializeField] public Collider dialogColl_;
        
        [Space(10)]
        [Header("Thrash")]
        public bool wasHit = false;
        public bool wasSeen = false;
        //MY LAST
        DS.ScriptableObjects.DSDialogueContainerSO quest;

        //MY LAST
        
        bool isUvorot = false;
        bool needSeeSplash = true;
        bool wasCitizien_ = false;
        int countOfDamages_ = 0;
        float nextThrowTime;
        float nextAttackTime;
        float maxHp_;
        RpgState rpgState_ = RpgState.Idle;
        ActionParent actionParent;
        NavMeshAgent agent;
        Animator animator;
        Transform player;
        private enum RpgState
        {
            Move,
            Idle,
            Death,
            Attack,
            Stalk,
            Throw,
            Block,
            Hurt
        }
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            actionParent = gameObject.AddComponent<ActionParent>();
            actionParent.doSmth = changeDialog;
            dialogColl_.GetComponent<ColliderStarter>().OnEnter += OnDialogEnter;
            dialogColl_.GetComponent<ColliderStarter>().OnExit += OnDialogExit;
        }
        void changeDialog()
        {
            if (quest != null)
            {
                MainHandler.Instance.StartQuestDialogGui(quest, new Mesh() , questHandler);
            }
            else
            {
                //TODO - Some text!!!
            }
        }
        private void OnDialogEnter(Collider col)
        {
            MainHandler.Instance.currentAction = actionParent;
        }
        private void OnDialogExit(Collider col)
        {
            if (MainHandler.Instance.currentAction == actionParent)
            {
                MainHandler.Instance.currentAction = null;
            }
        }
        public void checkQuestDialog()
        {
            if (!questHandler)
            {
                questHandler = GetComponent<DQuestTriggerParent>();
            }
            quest = questHandler.questContainer;
            if (quest != null && noDialog && !isTreater)
            {
                quest = null;
            }
            dialogColl_.enabled = false;
            if (citizen)
            {
                dialogColl_.enabled = true;
                if (quest != null)
                {
                    if (quest.name.ToLower() == "buy" || quest.name.ToLower() == "buyOrc")
                    {
                        _dialogAttention.SetActive(true);
                        var dialogBrain = _dialogAttention.GetComponent<DialogChanger>();
                        dialogBrain.setDialogPict(DialogType.gold);
                    }
                    else
                    {
                        if (!MainHandler.Instance.quests.ContainsKey(quest))
                        {
                            MainHandler.Instance.quests[quest] = quest.UngroupedDialogues[0];
                        }
                        var questAzyr = MainHandler.Instance.quests[quest];
                        int currLevel = int.Parse(questAzyr.name);
                        if (questAzyr != null && currLevel >= questHandler.startTrigger! && currLevel < questHandler.endTrigger! && !questAzyr.Final)
                        {
                            if (!noDialog)
                            {
                                _dialogAttention.SetActive(true);
                                var dialogBrain = _dialogAttention.GetComponent<DialogChanger>();
                                if (quest.MainQuest)
                                {
                                    if (questAzyr.Final)
                                    {
                                        dialogBrain.setDialogPict(DialogType.dialogMainPassive);
                                    }
                                    else
                                    {
                                        dialogBrain.setDialogPict(DialogType.dialogMain);
                                    }
                                }
                                else
                                {
                                    if (questAzyr.Final)
                                    {
                                        dialogBrain.setDialogPict(DialogType.dialogPassive);
                                    }
                                    else
                                    {
                                        dialogBrain.setDialogPict(DialogType.dialog);
                                    }
                                }
                            }
                        }
                        else
                        {
                            quest = null;
                        }
                    }
                }
            }
            else
            {
                quest = null;
            }
            if (quest == null)
            {
                _dialogAttention.SetActive(false);
            }
            else
            {
                _dialogAttention.SetActive(true);
            }
            if (quest == null && isTreater)
            {
                citizen = false;
                _shadowNpc.SetActive(false);
                wasHit = true;
                wasSeen = true;
            }
            if (citizen)
            {
                _shadowNpc.SetActive(true);
            }
        }
        virtual public void doBlood(Vector3 pos)
        {
            var temp = Instantiate(bloodParticles_, pos, Quaternion.identity, transform);
        }
        public bool internalPhysHurt(float hurt, bool inArmor, Vector3 pos, bool needCountDamage = true)
        {
            
            if (citizen && needCountDamage)
            {
                wasCitizien_ = true;
                countOfDamages_++;
                if (countOfDamages_ > 3 && quest == null)
                {
                    citizen = false;
                    _shadowNpc.GetComponent<ShadowFriend>().setFriend(false);
                    dialogColl_.enabled = false;
                }
                else
                {
                    //dialogBubble.Show("Айй, как больно, ты чего, в своём уме вообще?");
                    //createArghText();
                }
            }
            if (inArmor)
            {
                float dd = math.max(hurt * armor, 0);
                if (dd == 0)
                {
                    GameObject shield = Resources.Load<GameObject>("sprites/Prefabs/Effects/shield");
                    if (shield == null)
                    {
                        Debug.LogError("Не найден префаб shield в Resources/sprites/Prefabs/Effects/shield");
                    }
                    GameObject shieldInstance;
                    shieldInstance = Instantiate(shield, transform.position - new Vector3(0, 17 / 32, 0), Quaternion.identity);

                    //player.endHit();
                    return false;
                }
                if (!citizen)
                {
                    health -= dd;
                }
                if (health > 1)
                {
                    createSpecEffect();
                    doBlood(pos);
                }
            }
            else
            {
                if (!citizen)
                {
                    health -= hurt;
                }
                if (health > 1)
                {
                    createSpecEffect();
                }
            }
            if (health != maxHp_)
            {
                //gameRef.dbHandler.changeItemState(id: id, currentHp: health, worldName: _worldName);//TODO
            }
            return true;
        }
        virtual public bool needHurtAnim()
        {
            return true;
        }
        void createSpecEffect()
        {
            //flashWhite.Flash();
            _hitBar.SetHealth(health / maxHp_);

            // gameRef.gameMap?.skyTile.add(HitText(health.toStringAsFixed(0), position: Vector2(position.x, position.y + highQuest - 30)));
            // gameRef.gameMap!.container.add(ddText);
        }
        virtual public void doHurt(float hurt, Vector3 pos, bool inArmor = true, bool isPlayer = false)
        {
            if (rpgState_ == RpgState.Death)
            {
                return;
            }
            if (attackIcon.activeInHierarchy)
            {
                attackIcon.SetActive(false);
                if (!stanIcon.activeInHierarchy)
                {
                    stanIcon.SetActive(true);
                    animator.SetTrigger("hurt");
                    //TempEffect scaler = gameObject.AddComponent<TempEffect>();
                    //float scaleFactor = transform.localScale.x / 5f;

                    //scaler.Init(name: "debug", duration: 2, onFinish: () => { stanIcon.SetActive(false); });
                    //animachine.setAnimation(animachine.Hurt);
                    //animachine.setOnComplete(selectBehaviour);
                }
            }
            if (stanIcon.activeInHierarchy)
            {
                countSuperDash = 0;
                isUvorot = false;
                foreach(var coll in weaponColl_)
                {
                    coll.enabled = false;
                }
            }
            if (isPlayer)
            {
                wasSeen = true;
                needSeeSplash = false;
            }
            if (skipAllDamage())
            {
                return;
            }
            if (!internalPhysHurt(hurt, inArmor, pos) && !stanIcon.activeInHierarchy)
            {
                return;
            }
            //Blood(position: position.clone() + Vector2(0, dopPriority/2), isFlip: reversed)
            if (health < 1)
            {
                foreach (var coll in weaponColl_)
                {
                    coll.enabled = false;
                }
                death();
            }
            else
            {
                countSuperDash++;
                if (countSuperDash > 3 && !isUvorot)
                {
                    isUvorot = true;
                    CoroutineUtils.Start(this, () =>
                    {
                        countSuperDash = 0;
                        isUvorot = false;
                    }, 1.5f);
                    return;
                }
                if (isUvorot && !stanIcon.activeInHierarchy)
                {
                    return;
                }
                if (rpgState_ == RpgState.Attack)
                {
                    return;
                }
                if (!needHurtAnim())
                {
                    return;
                }
                foreach (var coll in weaponColl_)
                {
                    coll.enabled = false;
                }
                animator.SetTrigger("hurt");
                rpgState_ = RpgState.Hurt;
            }
        }
        virtual public void death()
        {
            if (rpgState_ == RpgState.Death)
            {
                return;
            }
            rpgState_ = RpgState.Death;
            //if (deathMedia.Count > 0) //TODO Потом добавить музыку
            //{
            //    int idx = UnityEngine.Random.Range(0, deathMedia.Count);
            //    MainHandler.Instance.playSmartSmallSound(source: deathMedia[idx], volume: 1
            //        , pos: transform.position);
            //}
            if (!wasCitizien_ || isTreater)
            {
                for (int i = 0; i < maxHp_ * 4; i += 40)
                {
                    Instantiate(crystall_, transform.position, Quaternion.identity);
                }
            }
            //speed.x = 0;
            //speed.y = 0;
            //rigidBody_.simulated = false;
            for (int i = 0; i < loots!.Count; i++)
            {
                Instantiate(loots![i], transform.position - new Vector3(i * 15 / 32, 0, 0), Quaternion.identity);
            }

            foreach(var coll in hitBoxColl_){
                coll.enabled = false;
            }
            foreach (var coll in weaponColl_)
            {
                coll.enabled = false;
            }
            dialogColl_.enabled = false;
            //playerChecker_.enabled = false;

            animator.SetTrigger("death");

            //animachine.setAnimation(animachine.Death);
            //animachine.currentAnimator().onComplete = () =>
            //{
                //var temp = gameObject.AddComponent<Hider>();
                //temp.TotalHide(scale: false, duration: 1);
            //};
            //if (id > -1)
            //{
                //gameRef.dbHandler.changeItemState(id: id,
                //    worldName: _worldName,
                //    used: true);
            //}
        }
        virtual public bool skipAllDamage()
        {
            return false;
        }
        private void Update()
        {
            if (rpgState_ == RpgState.Death) return;
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("attack") || stateInfo.IsName("attack2")
                || stateInfo.IsName("throw"))
            {
                rpgState_ = RpgState.Attack;
                wasHit = true;
            }
            else if (stateInfo.IsName("block"))
            {
                rpgState_ = RpgState.Block;
            }
            else if (stateInfo.IsName("hurt"))
            {
                rpgState_ = RpgState.Hurt;
            }
            else if (stateInfo.IsName("idle") || stateInfo.IsName("funnyIdle"))
            {
                rpgState_ = RpgState.Idle;
            }
            else if (stateInfo.IsName("walk"))
            {
                rpgState_ = RpgState.Move;
            }
            else if (stateInfo.IsName("death"))
            {
                rpgState_ = RpgState.Death;
                return;
            }
            if (!citizen)
            {
                FindPlayer(stateInfo);
            }
            else
            {
                LiveRandom(stateInfo);
            }

            if (player == null)
            {
                SwitchToState(RpgState.Idle);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            switch (rpgState_)
            {
                case RpgState.Idle:
                    HandleIdleState(distanceToPlayer);
                    break;
                case RpgState.Stalk:
                    HandleChaseState(distanceToPlayer);
                    break;
                case RpgState.Attack:
                    // Handled by transition or timer
                    break;
                case RpgState.Throw:
                    // Handled by transition or timer
                    break;
            }

            UpdateAnimations();
        }

        void LiveRandom(AnimatorStateInfo animState)
        {
            if(rpgState_ != RpgState.Idle && rpgState_ != RpgState.Move)
            {
                return;
            }
            if (animState.normalizedTime >= 0.9)
            {
                var isMoving = UnityEngine.Random.Range(0, 2) == 0;
                animator.SetBool("walk", isMoving);
                animator.SetBool("idle", !isMoving);
            }
        }
        private void FindPlayer(AnimatorStateInfo stateInfo)
        {
            if (player != null)
            {
                LiveRandom(stateInfo);
                return;
            }

            Collider[] targets = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);
            if (targets.Length > 0)
            {
                player = targets[0].transform;
            }
        }

        private void HandleIdleState(float distance)
        {
            if (distance <= detectionRange)
            {
                SwitchToState(RpgState.Stalk);
            }
        }

        private void HandleChaseState(float distance)
        {
            if (!agent.isOnNavMesh) return;

            // Stop moving if within attack distance
            if (distance <= attackDistance)
            {
                SafeSetStopped(true);
                FacePlayer();
            }
            else
            {
                agent.SetDestination(player.position);
                SafeSetStopped(false);
            }

            if (distance <= attackDistance)
            {
                if (Time.time >= nextAttackTime)
                {
                    // Randomly decide to idle instead of attacking
                    if (UnityEngine.Random.Range(0, 101) < idleProbability)
                    {
                        SwitchToState(RpgState.Idle);
                        nextAttackTime = Time.time + 1f; // Brief pause before next decision
                    }
                    else
                    {
                        PerformAttack();
                    }
                }
            }
            else if (canThrow && distance <= throwDistance && Time.time >= nextThrowTime)
            {
                PerformThrow();
            }
        }

        private void PerformAttack()
        {
            rpgState_ = RpgState.Attack;
            SafeSetStopped(true);
            FacePlayer();
            animator.SetTrigger("attack");
            nextAttackTime = Time.time + attackCooldown;
            // The animator state machine should return to walk/idle or we can resume chase after some time
            Invoke(nameof(ResumeChase), 1.5f);
        }

        private void PerformThrow()
        {
            rpgState_ = RpgState.Throw;
            SafeSetStopped(true);
            animator.SetTrigger("throw");
            nextThrowTime = Time.time + throwCooldown;
            // The projectile instantiation should ideally be handled by an Animation Event
            // But for now, we resume chase after some time
            Invoke(nameof(ResumeChase), 2f);
        }

        private void ResumeChase()
        {
            if (rpgState_ == RpgState.Death) return;
            rpgState_ = RpgState.Stalk;
        }

        private void SwitchToState(RpgState newState)
        {
            rpgState_ = newState;
            if (newState == RpgState.Idle || newState == RpgState.Attack || newState == RpgState.Throw)
            {
                SafeSetStopped(true);
            }
            else
            {
                SafeSetStopped(false);
            }
        }

        private void SafeSetStopped(bool stopped)
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = stopped;
            }
        }

        private void FacePlayer()
        {
            if (player == null) return;

            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0; // Keep the giant upright

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            }
        }

        private void UpdateAnimations()
        {
            if (agent == null || animator == null) return;

            bool isNavMeshActive = agent.isActiveAndEnabled && agent.isOnNavMesh;
            bool isMoving = isNavMeshActive && agent.hasPath && agent.remainingDistance > 0.1f && !agent.isStopped;

            animator.SetBool("walk", isMoving);
            animator.SetBool("idle", !isMoving);
        }

        // Potential Animation Event Call
        public void LaunchProjectile()
        {
            if (projectilePrefab != null && throwPoint != null)
            {
                GameObject proj = Instantiate(projectilePrefab, throwPoint.position, throwPoint.rotation);
                // Add velocity to proj if needed, e.g. proj.GetComponent<Rigidbody>().velocity = ...
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackDistance);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, throwDistance);
        }
    }
}
