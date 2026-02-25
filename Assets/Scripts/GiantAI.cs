using System.Collections;
using KinematicCharacterController.Examples;
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

            foreach(var coll in weaponColl_)
            {
                coll.GetComponent<ColliderStarter>().OnEnter += doHit;
            }
            maxHp_ = health;
        }
        private void OnDestroy()
        {
            foreach (var coll in weaponColl_)
            {
                coll.GetComponent<ColliderStarter>().OnEnter -= doHit;
            }
        }
        void doHit(Collider other)
        {
            ExampleCharacterController player = other.transform.root.GetComponentInParent<ExampleCharacterController>();
            if (player != null)
            {
                MainHandler.Instance.addHealth(-10, DamageType.Phys);
            }
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
                    //GameObject shield = Resources.Load<GameObject>("sprites/Prefabs/Effects/shield");
                    //if (shield == null)
                    //{
                        //Debug.LogError("Не найден префаб shield в Resources/sprites/Prefabs/Effects/shield");
                    //}
                    //GameObject shieldInstance;
                    //shieldInstance = Instantiate(shield, transform.position - new Vector3(0, 17 / 32, 0), Quaternion.identity);

                    //player.endHit();
                    return false;
                }
                if (!citizen)
                {
                    health -= dd;
                }
                if (health > 1)
                {
                    createSpecEffect(pos);                    
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
                    createSpecEffect(pos);
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
        void createSpecEffect(Vector3 pos)
        {
            var temp = Instantiate(bloodParticles_, pos, Quaternion.identity);
            _hitBar.SetHealth(health / maxHp_);
            // gameRef.gameMap?.skyTile.add(HitText(health.toStringAsFixed(0), position: Vector2(position.x, position.y + highQuest - 30)));
            // gameRef.gameMap!.container.add(ddText);
        }
        public void CallNearEnemies()
        {
            if (wasSeen) return;
            wasSeen = true;

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange - detectionRange/3);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject == gameObject) continue;
                
                GiantAI neighbor = hitCollider.transform.root.GetComponent<GiantAI>();
                if (neighbor != null)
                {
                    neighbor.CallNearEnemies();
                }
            }
        }
        virtual public void doHurt(float hurt, Vector3 pos, bool inArmor = true, bool isPlayer = false)
        {
            if (rpgState_ == RpgState.Death)
            {
                return;
            }
            if(isPlayer)
            {
                CallNearEnemies();
                wasSeen = true;
                needSeeSplash = false;
            }
            if (attackIcon.activeInHierarchy)
            {
                attackIcon.SetActive(false);
                if (!stanIcon.activeInHierarchy)
                {
                    stanIcon.SetActive(true);
                    animator.SetTrigger("hurt");
                    Invoke(nameof(HideStanIcon), 2f);
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
            StartCoroutine(FadeAndDestroy());
        }
        
        private IEnumerator FadeAndDestroy()
        {
            yield return new WaitForSeconds(3f);

            // Cleanup physics to allow sinking
            if (agent != null) agent.enabled = false;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            Collider mainColl = GetComponent<Collider>();
            if (mainColl != null) mainColl.enabled = false;

            float duration = 2f;
            float elapsed = 0f;
            Vector3 initialScale = transform.localScale;
            Vector3 targetScale = Vector3.zero;
            Vector3 initialPos = transform.position;
            Vector3 targetPos = initialPos + Vector3.down * 1.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
                transform.position = Vector3.Lerp(initialPos, targetPos, t);

                yield return null;
            }

            Destroy(gameObject);
        }
        virtual public bool skipAllDamage()
        {
            return false;
        }
        private void Update()
        {
            if (rpgState_ == RpgState.Death) return;
            
            if (stanIcon != null && stanIcon.activeInHierarchy)
            {
                SafeSetStopped(true);
                return; // Do nothing while stunned
            }

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
            
            FindPlayer(stateInfo);

            if (rpgState_ == RpgState.Attack || rpgState_ == RpgState.Block || rpgState_ == RpgState.Hurt || rpgState_ == RpgState.Throw)
            {
                // Let animation finish before choosing behavior
                return;
            }

            SelectBehaviour(stateInfo);
            UpdateAnimations();
        }

        private float nextDecisionTime = 0f;

        private void SelectBehaviour(AnimatorStateInfo stateInfo)
        {
            if (Time.time < nextDecisionTime) return;
            
            nextDecisionTime = Time.time + (!wasSeen && !wasHit ? 1 : 0.2f);

            if (citizen)
            {
                MoveIdleRandom(false);
                return;
            }

            if (wasHit)
            {
                if (player != null)
                {
                    FacePlayer();
                    float distanceToPlayer = Vector3.Distance(transform.position, player.position);

                    if (distanceToPlayer <= attackDistance)
                    {
                        if (Time.time >= nextAttackTime)
                        {
                            if (UnityEngine.Random.Range(0, 101) < idleProbability)
                            {
                                SwitchToState(RpgState.Idle);
                                nextAttackTime = Time.time + 1f;
                            }
                            else
                            {
                                PerformAttack();
                            }
                        }
                        else
                        {
                            SwitchToState(RpgState.Idle);
                        }
                    }
                    else
                    {
                        HandleChaseState();
                    }
                }
                else
                {
                    MoveIdleRandom(true);
                }
            }
            else 
            {
                MoveIdleRandom(wasSeen);
            }
        }

        private void MoveIdleRandom(bool isSee)
        {
            int random = UnityEngine.Random.Range(0, 2);
            if (random != 0 || wasHit)
            {
                SafeSetStopped(false); 
                if (isSee && player != null)
                {
                    Vector3 targetPos = player.position;
                    Vector3 direction = (transform.position - player.position).normalized;
                    if (direction != Vector3.zero)
                    {
                        targetPos += direction * (attackDistance * 0.5f);
                    }
                    agent.SetDestination(targetPos);

                    float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                    if (distanceToPlayer < attackDistance)
                    {
                        wasHit = true;
                    }
                }
                else
                {
                    if (agent.isOnNavMesh && (!agent.hasPath || agent.remainingDistance < 0.5f))
                    {
                        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * 10f;
                        Vector3 randomPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                        
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(randomPos, out hit, 10f, NavMesh.AllAreas))
                        {
                            agent.SetDestination(hit.position);
                        }
                    }
                }
            }
            else
            {
                SwitchToState(RpgState.Idle);
            }
            
        }

        private void FindPlayer(AnimatorStateInfo stateInfo)
        {
            if (player != null) return;

            Collider[] targets = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);
            foreach (var target in targets)
            {
                // Check if target is behind an obstacle
                Vector3 origin = transform.position + Vector3.up * 1f; // Offset from ground 
                Vector3 targetCenter = target.bounds.center;
                
                Vector3 dir = targetCenter - origin;
                float dist = dir.magnitude;
                int combinedMask = playerLayer.value | LayerMask.GetMask("Default");
                
                RaycastHit[] hits = Physics.RaycastAll(origin, dir.normalized, dist, combinedMask, QueryTriggerInteraction.Collide);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                
                bool canSeePlayer = false;
                foreach (var h in hits)
                {
                    if (h.transform.IsChildOf(transform)) continue; // ignore self
                    
                    bool isPlayerLayer = (playerLayer.value & (1 << h.collider.gameObject.layer)) != 0;
                    
                    if (isPlayerLayer || h.transform.IsChildOf(target.transform))
                    {
                        canSeePlayer = true;
                        break;
                    }
                    
                    // IF we hit something else on Default (like a wall or another enemy), LOS is blocked
                    // BUT ignore it if it's a trigger, because we only want solid objects to block LOS
                    if (h.collider.isTrigger) continue;
                    
                    break;
                }
                
                if (!canSeePlayer) continue;

                Vector3 directionToTarget = target.transform.position - transform.position;
                directionToTarget.y = 0;
                if (directionToTarget != Vector3.zero) directionToTarget.Normalize();
                
                Vector3 forward = transform.forward;
                forward.y = 0;
                if (forward != Vector3.zero) forward.Normalize();
                
                float angle = Vector3.Angle(forward, directionToTarget);
                if (angle <= 55f) // 110 degrees total (55 degrees left and right)
                {
                    player = target.transform;
                    if (!wasSeen && needSeeSplash)
                    {
                        if (_dangerSaw != null) _dangerSaw.SetActive(true);
                        CallNearEnemies();
                        Invoke(nameof(HideDangerSaw), 1.5f);
                    }
                    wasSeen = true;
                    break;
                }
            }
        }


        private void HandleChaseState()
        {
            if (!agent.isOnNavMesh) return;
            if (player == null) return;
            
            Vector3 targetPos = player.position;
            Vector3 direction = (transform.position - player.position).normalized;
            if (direction != Vector3.zero)
            {
                targetPos += direction * (attackDistance * 0.5f);
            }
            agent.SetDestination(targetPos);
            SafeSetStopped(false);
            
            float distance = Vector3.Distance(transform.position, player.position);
            if (canThrow && distance <= throwDistance && distance > attackDistance && Time.time >= nextThrowTime)
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
            
            // Attack Icon Logic (Random 1 in 3 chance)
            if (UnityEngine.Random.Range(0, 3) == 0 && attackIcon != null)
            {
                attackIcon.SetActive(true);
                Invoke(nameof(HideAttackIcon), 1f); // Adjust duration if needed based on animation length
            }

            nextAttackTime = Time.time + attackCooldown;
        }

        private void HideDangerSaw()
        {
            if (_dangerSaw != null) _dangerSaw.SetActive(false);
        }

        private void HideAttackIcon()
        {
            if (attackIcon != null) attackIcon.SetActive(false);
        }

        private void HideStanIcon()
        {
            if (stanIcon != null) stanIcon.SetActive(false);
            // Optionally clear hurt state if we want next decision to take over immediately
            nextDecisionTime = Time.time; 
        }

        private void PerformThrow()
        {
            rpgState_ = RpgState.Throw;
            SafeSetStopped(true);
            FacePlayer();
            animator.SetTrigger("throw");
            nextThrowTime = Time.time + throwCooldown;
        }

        private void SwitchToState(RpgState newState)
        {
            rpgState_ = newState;
            if (newState == RpgState.Idle || newState == RpgState.Attack || newState == RpgState.Throw || newState == RpgState.Block || newState == RpgState.Hurt)
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
            bool isMoving = isNavMeshActive && agent.hasPath && agent.remainingDistance > attackDistance/2 && !agent.isStopped;

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
