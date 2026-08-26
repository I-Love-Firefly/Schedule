# 国内高校公开课程表测试集

本目录中的图片仅用于课程表识别回归测试。所有样本均由中国高校官网公开发布的 PDF 渲染而来，采集日期为 2026-08-02。原 PDF 仅作为临时渲染输入，不纳入项目。

| 文件 | 学校 | 原 PDF 页面 | 结构特点 | 官方来源 |
| --- | --- | ---: | --- | --- |
| `01_scut_class_schedule.png` | 华南理工大学 | 1 | 英文星期表头、左侧完整时间段、课程名/教室/教师/周次混排 | https://www2.scut.edu.cn/_upload/article/files/62/54/2270160b47ed9b4469f2a1e2905a/39459227-068d-47bc-aa9e-fc1b3b1f9794.pdf |
| `02_jnu_class_schedule.png` | 暨南大学 | 1 | 中文星期表头、节次轴、周一至周日、同一格多门课程 | https://ms.jnu.edu.cn/_upload/article/files/45/a5/5d433cae4be5822981081cf6b084/07f2bee1-3367-40ed-9e3d-6a1a4cbdd330.pdf |
| `03_nju_department_schedule.png` | 南京大学 | 1 | 上半页课程信息表、下半页周课表、纯数字节次、合并单元格 | https://jw.nju.edu.cn/_upload/article/files/fb/2c/1549b3e240d293ad8160cfabf1c1/6091630c-32de-4eb8-a17d-3d0fdfae1b64.pdf |
| `04_shnu_class_schedule.png` | 上海师范大学 | 7 | 星期日开头、14 节纵轴、课程代码、多行地点、跨节课程 | https://jwc.shnu.edu.cn/_upload/article/files/e8/a0/1f90dfd24f60a78b86da5bdea290/725fb349-adfa-4b07-b54e-f4ddb01ca618.pdf |
| `05_ujn_graduate_schedule.png` | 济南大学 | 19 | 节次和时间同时存在、课程跨多个行块、多教师分周授课 | https://yjs.ujn.edu.cn/__local/3/31/09/CF141D84AC26A15D2D9894470E0_A0EE62C5_82350.pdf |
| `06_uestc_graduate_schedule.png` | 电子科技大学 | 13 | 非周视图课程清单，星期与节次使用纵向合并单元格，作为边界/拒识测试 | https://gr.uestc.edu.cn/attached/papers/116/201901/20190118100008_69667.pdf |

## 建议测试指标

1. 是否正确识别星期列以及时间或节次轴。
2. 课程召回率：原图课程中成功生成记录的比例。
3. 字段准确率：课程名、星期、起止时间、地点、教师分别统计。
4. 合并单元格、单双周和同格多课程是否被错误合并。
5. 对不属于个人周课表的清单型图片，应提示用户确认或拒绝自动写入，不能生成大量错误课程。
