public enum WeaponType
{
    Buster,
    Machinegun,
    Blade,
    Cutter,
    Sawblade,
    BeamSaber,
    Shotgun,
    Flamethrower,
    Grinder,
    ForceFieldGenerator,
    Equipment
}

public enum EquipmentType
{
    None,           // Equipment가 아닌 일반 무기일 때
    PowerUpper,     // 공격력 증가
    SpeedUpper,     // 이동속도 증가
    HealthUpper,    // 체력 증가
    HasteUpper,     // 쿨다운 감소
    PortableMagnet, // 아이템 획득 범위 증가
    KnockbackUpper, // 넉백 증가
    RegenUpper      // 체력 재생 증가
}