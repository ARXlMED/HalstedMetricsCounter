fn sum_scores(scores: &[i32]) -> i32 {
    let mut total = 0;
    for s in scores {
        total = total + *s;
    }
    return total;
}
fn average(scores: &[i32]) -> f64 {
    let total = sum_scores(scores);
    let n = scores.len() as f64;
    return total as f64 / n;
}
fn maximum(scores: &[i32]) -> i32 {
    let mut best = scores[0];
    for s in scores {
        if *s > best {
            best = *s;
        }
    }
    return best;
}
fn minimum(scores: &[i32]) -> i32 {
    let mut worst = scores[0];
    for s in scores {
        if *s < worst {
            worst = *s;
        }
    }
    return worst;
}
fn count_fail(scores: &[i32]) -> i32 {
    let mut n = 0;
    for s in scores {
        if *s <= 4 {
            n = n + 1;
        }
    }
    return n;
}
fn scholarship(avg: f64) -> f64 {
    if avg >= 9.0 {
        return 321.65;
    } else if avg >= 8.0 {
        return 281.44;
    } else if avg >= 6.0 {
        return 241.24;
    } else if avg >= 5.0 {
        return 201.03;
    } else {
        return 0.0;
    }
}
fn main() {
    let names = ["Анна", "Борис", "Виктор", "Галина", "Дмитрий"];
    let scores = [
        [10, 9, 10, 9, 10],
        [5, 4, 6, 5, 4],
        [7, 8, 7, 6, 8],
        [9, 10, 9, 10, 9],
        [3, 4, 2, 5, 4],
    ];
    let mut best_avg = 0.0;
    let mut best_index = 0;
    let mut i = 0;
    while i < names.len() {
        let name = names[i];
        let row = &scores[i];
        let total = sum_scores(row);
        let avg = average(row);
        let best = maximum(row);
        let worst = minimum(row);
        let fails = count_fail(row);
        let money = scholarship(avg);
        println!("=== {} ===", name);
        println!("Оценки: {:?}", row);
        println!("Сумма баллов: {}", total);
        println!("Средний балл: {:.2}", avg);
        println!("Лучшая оценка: {}", best);
        println!("Худшая оценка: {}", worst);
        println!("Неудов: {}", fails);
        println!("Стипендия: {:.2} руб.", money);
        if avg > best_avg {
            best_avg = avg;
            best_index = i;
        }
        i = i + 1;
    }
    println!("--- Итог группы ---");
    println!("Лучший студент: {}", names[best_index]);
    println!("Средний балл: {:.2}", best_avg);
}