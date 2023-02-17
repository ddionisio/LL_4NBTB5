
public enum AttackState {
    None,
    Attacking, //in the process through modals
    Fail, //mistake count reached
    Cancel, //backed off
    Success //successfully computed the number, perform attack
}
