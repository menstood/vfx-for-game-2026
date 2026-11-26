# ประกาศ Final

Final ปีนี้เป็น VFX Showreel ครับ มี Archer ยิงสกิลใส่ Dummy ทั้งหมด 5 สกิล เราเตรียม Animation, กล้อง กับจังหวะการยิงไว้ให้หมดแล้ว งานของทุกคนคือทำ VFX ใส่ลงไปให้แต่ละสกิลดูเท่ที่สุด แล้วก็จัดฉากให้เข้ากัน

## สิ่งที่ต้องทำ

- VFX ครบทั้ง 5 สกิล สกิลละ 4 จังหวะ (Circle, Cast, Projectile, Impact)
- Shader ใส่ได้เต็มที่ ทั้ง Shader Graph และ Particle
- Texture ทำเองหรือหามาได้หมด
- ใช้ Mesh ที่สร้างขึ้นมาเองได้ ใช้ Pro Builder ได้
- Paint Terrain (ตอนนี้ Terrain ยังไม่มี Texture เลย)
- จัด Scene ใน Folder `Environment/Rock` มีหินให้ไว้ 20 แบบ จะเอามาวางหรือหามาเพิ่มก็ได้
- จัดแสง ใส่ Skybox ใส่ Fog
- Volume ใช้ได้หมดเลย
- VFX Graph ใช้ได้ถ้าอธิบายให้เราเข้าใจได้

## สิ่งที่ห้ามทำ

- Timeline
- Third party ของคนอื่น

## HOW TO

- Unity Version 6000.3.13f1 (ใช้เวอร์ชั่นสูงกว่านี้ได้ แต่ต่ำกว่านี้อาจจะมีปัญหา)
- เปิด Scene `Assets/Scenes/Showreel.unity` แล้วกด Play ได้เลย Archer จะยิงวนครบ 5 สกิลไปเรื่อยๆ
- ทำตรงข้างล่าง
  - ===Do=== นะครับ
  - ===Dont=== อย่าไปยุ่งกับมัน
  - `Environment` จัดได้เต็มที่

### สกิลทั้ง 5

| สกิล | ท่า | ยิงไปไหน |
|---|---|---|
| Skill01 | ง้างธนู เล็ง แล้วยิง | ธนู → Dummy |
| Skill02 | ท่าเดียวกับ Skill01 แต่ VFX ต้องเป็นอีกแบบนะ | ธนู → Dummy |
| Skill03 | เอนตัวยิงขึ้นฟ้า | ยิงขึ้นฟ้าหลุดจอไป แล้วตกลงมาที่พื้นตรง Dummy |
| Skill04 | หมุนตัว 360 องศาแล้วนั่งยิง | ธนู → Dummy (มุมต่ำ) |
| Skill05 | กระโดดขึ้นฟ้า ยิงลงมา แล้วลงพื้น | ธนูจากกลางอากาศ → พื้นตรง Dummy |

### VFX แต่ละจังหวะ

แต่ละสกิลมี Object 4 ตัวให้ใส่ VFX

| Object | เล่นตอนไหน | ทิศ |
|---|---|---|
| `Circle` | วงเวทใต้เท้า Archer ตั้งแต่เริ่มสกิลจนยิง | วางราบกับพื้น (Y ชี้ขึ้น) แกน Z ชี้ไปทางเป้า |
| `Cast` | ติดอยู่ที่ธนูตั้งแต่เริ่มสกิลจนยิง | แกน Z ชี้ไปทางที่ยิง |
| `Projectile` | ลูกที่บินไปหาเป้า | แกน Z ชี้ไปทางที่บิน |
| `Impact` | ตอนโดนเป้า (1.5 วินาที) | แกน Z ชี้กลับไปทางที่ลูกมา (ถ้าตกพื้นก็ชี้ขึ้นฟ้า) |

วิธีใส่

1. กาง Skill ใน Hierarchy เช่นกาง Skill01 แล้วเลือก `Cast` (ตอนยังไม่กด Play มันจะถูกปิดไว้ ติ๊กเปิดดูตอนทำได้)
2. เอา VFX ใส่ไว้ใน Object นั้น ข้างๆ `Placeholder`
3. ทำเสร็จแล้วปิดหรือลบ `Placeholder` ทิ้งได้เลย
4. อย่าเอา VFX ไปใส่ข้างใน `Placeholder` นะครับ มันโดน Scale ไว้ ของข้างในจะเบี้ยว
5. ไม่ต้องจัดตำแหน่งเอง Script จะย้ายไปที่ธนูหรือที่เป้าให้ทุกครั้งที่เล่น

Object จะถูกเปิดใหม่ทุกครั้งที่เล่น อะไรที่เริ่มตอน Enable ก็จะเล่นใหม่ทุกรอบ เช่น Animator / Animation Clip (Animate Scale หรือค่าใน Material ได้), Particle System (Play On Awake), VFX Graph

### ปุ่มตอนกด Play

- `1` ถึง `5` เล่นทีละสกิล
- `Space` เริ่มวนใหม่ตั้งแต่สกิลแรก

### กล้อง

กล้องจะเป็นแบบเกม Turn Based คือตัดไปมุมกว้างของแต่ละสกิล แล้วนิ่งค้างไว้จนยิงโดนเสร็จ ระหว่างสกิลจะตัดกลับมามุม `Wide` ที่ค่อยๆ เลื่อนไปด้านข้าง

อยากเปลี่ยนมุมกล้องก็ได้ หมุน Scene View ไปมุมที่อยากได้ เลือกตัว Locator ของสกิลนั้นใน `CameraShots` แล้วกด **GameObject > Align With View** (Ctrl+Shift+F) ขอให้ยังเห็น Archer, ทางที่ลูกบิน แล้วก็ตอนโดนเป้าอยู่ในจอนะครับ

### Environment

- Terrain ปั้นเนินไว้ให้แล้ว แต่ยังไม่มี Texture เลือก `Environment > Terrain` ไปที่ **Paint Terrain > Paint Texture** กด **Edit Terrain Layers > Add Layer** แล้วเลือกจาก `Environment/Terrain/Layers` (Grass, Dirt, Stone, Sand) หรือทำ Layer เองก็ได้ Layer แรกที่ใส่จะเต็มทั้ง Terrain ที่เหลือค่อย Paint ทับ
- Skybox มีให้ 3 แบบใน `Environment/SkyBox` (`Sky_DuskClouds`, `Sky_Night`, `Sky_Storm`) ลากใส่ Scene View หรือไปตั้งที่ **Window > Rendering > Lighting > Environment**
- Fog ไปเปิดที่ **Window > Rendering > Lighting > Environment > Other Settings > Fog**
- Bloom อยู่ใน Global Volume สีของ VFX ที่ Intensity เกิน 1 (HDR) จะเรืองแสง

### ปรับค่าสกิล

เลือก `SkillShowcase` ใน Scene

- ปรับแต่ละสกิลได้ `Release Time` (ยิงตอนวินาทีที่เท่าไหร่), `Projectile Speed`, `Impact Duration`
- `Rain Height` ความสูงที่ลูกของ Skill03 เริ่มตกลงมา
- เปลี่ยนชื่อสกิลได้ที่ `Skill Name`

กำหนดส่ง (ใส่วันที่)
