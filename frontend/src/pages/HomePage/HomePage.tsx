import { Link } from "react-router";
import { useAuth } from "../../hooks/useAuth";
import { useHealth } from "../../hooks/useHealth";
import { HeroDemo } from "./HeroDemo";
import { HeroSteps } from "./HeroSteps";
import { HeroTitle } from "./HeroTitle";

export function HomePage() {
  const { user, isLoading } = useAuth();
  // Chỉ để đánh thức backend (host free ngủ sau 15 phút): không render kết quả, lỗi cũng không hiện.
  useHealth();

  return (
    <div className="relative isolate overflow-hidden">
      <div className="hero-glow" aria-hidden="true" />
      <main className="mx-auto flex min-h-dvh max-w-5xl flex-col justify-center gap-10 px-4 py-12">
        <div className="grid items-center gap-10 lg:grid-cols-2">
          <header className="space-y-3">
            <HeroTitle />
            <p
              className="hero-rise text-muted"
              style={{ "--d": "0.7s" } as React.CSSProperties}
            >
              Quiz real-time: nhập chủ đề, AI sinh câu hỏi, bạn bè vào phòng
              bằng mã và thi đấu trực tiếp.
            </p>
            {!isLoading && (
              <div
                className="hero-actions hero-rise flex flex-wrap gap-3.5"
                style={{ "--d": "0.9s" } as React.CSSProperties}
              >
                {user ? (
                  <Link className="btn btn-primary" to="/dashboard">
                    Vào trang của tôi
                  </Link>
                ) : (
                  <>
                    <Link className="btn btn-primary" to="/login">
                      Đăng nhập
                    </Link>
                    <Link className="btn btn-ghost" to="/register">
                      Đăng ký
                    </Link>
                  </>
                )}
                <Link className="btn btn-ghost" to="/join">
                  Vào phòng
                </Link>
                <Link className="btn btn-ghost" to="/shop">
                  Cửa hàng
                </Link>
                <Link className="btn btn-ghost" to="/ranking">
                  Xếp hạng
                </Link>
              </div>
            )}
          </header>
          <HeroDemo />
        </div>
        <HeroSteps />
      </main>
    </div>
  );
}
