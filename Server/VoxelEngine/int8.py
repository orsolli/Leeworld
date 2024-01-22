def to8Adic(num: float):
    """
    Convert to 8-adic number,

    a big-endian number. Least significant number first. "671" means 6/128 + 7/64 + 1/8

    Args:
        num (float): A number less than 1

    >>> [to8Adic(n) for n in [0.0, 0.125, 0.25, 0.375, 0.5, 0.625]]
    ["000", "001", "002", "003", "004", "005"]
    >>> to8Adic(0.046875)
    "030"
    >>> to8Adic(0.546875)
    "034"
    >>> to8Adic(1)
    Traceback (most recent call last):
        ...
    ValueError: |num| must be < 1

    """
    res = ""
    if num == 0:
        return "0"
    sign = num / abs(num)
    num = abs(num)
    num *= 8
    while num >= int(num) and len(res) < 3:
        if num < 1 / 512:
            break
        i = int(num)
        res = f"{i}{res}"
        num -= i
        num *= 8

    if len(res) == 0:
        return "0"
    return ("-" if sign < 0 else "") + res


def from8Adic(num: str):
    """
    Convert from 8-adic number,

    Args:
        num (str): A big-endian number. Least significant number first. "671" means 6/128 + 7/64 + 1/8

    >>> [from8Adic(str(n)) for n in range(6)]
    [0.0, 0.125, 0.25, 0.375, 0.5, 0.625]
    >>> from8Adic("30")
    0.046875
    >>> from8Adic("034")
    0.546875
    >>> from8Adic("-1")
    Traceback (most recent call last):
        ...
    ValueError: num must be >= 0

    """
    n = 0.0
    while num:
        n = n / 8 + int(num[0], base=8)
        num = num[1:]
    return n / 8
